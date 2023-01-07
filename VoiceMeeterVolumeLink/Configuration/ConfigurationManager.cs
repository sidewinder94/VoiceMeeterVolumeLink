using System.IO;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.Extensions.Logging;
using VoiceMeeterVolumeLink.Async;

namespace VoiceMeeterVolumeLink.Configuration;

public class ConfigurationManager : IConfigurationManager
{
    private const string UserSettingsFileName = "usersettings.json";

    private static string ConfigurationPath;

    private readonly ILogger<ConfigurationManager> _logger;
    private readonly AsyncLazy<RootConfiguration> _configuration;
    private readonly SemaphoreSlim _saveSemaphore = new(1);

    static ConfigurationManager()
    {
        string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            
        string? directory = Path.GetDirectoryName(assemblyLocation);

        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Path.GetPathRoot(assemblyLocation);
        }

        ConfigurationPath = Path.Combine(directory, UserSettingsFileName);
    }
    
    public ConfigurationManager(ILogger<ConfigurationManager> logger)
    {
        this._logger = logger;
        this._configuration =
            new AsyncLazy<RootConfiguration>(this.ReadConfigurationInternal);
    }

    public async Task<RootConfiguration> GetConfigurationAsync()
    {
        return await this._configuration;
    }

    public async Task SaveConfigurationAsync()
    {
        await this._saveSemaphore.WaitAsync();

        try
        {
            await using var configFile = File.Open(ConfigurationPath, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                FileShare.Read);

            await JsonSerializer.SerializeAsync(configFile, await this._configuration,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                });

            configFile.SetLength(configFile.Position);
            await configFile.FlushAsync();
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "Error while saving user settings");
        }
        finally
        {
            this._saveSemaphore.Release();
        }
    }

    private async Task<RootConfiguration> ReadConfigurationInternal()
    {
        if (!File.Exists(ConfigurationPath)) return new RootConfiguration();

        try
        {
            await using var configFile =
                File.Open(ConfigurationPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
            return (await JsonSerializer.DeserializeAsync<RootConfiguration>(configFile)) ?? new RootConfiguration();
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "Error while loading user settings");
            return new RootConfiguration();
        }
    }
}
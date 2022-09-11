using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using VoiceMeeterVolumeConfiguration.Async;

namespace VoiceMeeterVolumeConfiguration.Configuration;

public class ConfigurationManager : IConfigurationManager
{
    private const string UserSettingsFileName = "usersettings.json";
    
    private readonly ILogger<ConfigurationManager> _logger;
    private readonly AsyncLazy<RootConfiguration> _configuration;
    private readonly SemaphoreSlim _saveSemaphore = new(1);

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
            await using var configFile = File.Open(UserSettingsFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                FileShare.Read);
            
            await JsonSerializer.SerializeAsync(configFile, await this._configuration,
                new JsonSerializerOptions
                {
                    WriteIndented = true
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
        if (!File.Exists(UserSettingsFileName)) return new RootConfiguration();

        try
        {
            await using var configFile = File.Open(UserSettingsFileName, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
            return (await JsonSerializer.DeserializeAsync<RootConfiguration>(configFile)) ?? new RootConfiguration();
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "Error while loading user settings");
            return new RootConfiguration();
        }
    }
    
}
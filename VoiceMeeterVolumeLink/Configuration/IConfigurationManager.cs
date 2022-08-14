using System.Threading.Tasks;

namespace VoiceMeeterVolumeConfiguration.Configuration;

public interface IConfigurationManager
{
    Task<RootConfiguration> GetConfigurationAsync();
    Task SaveConfigurationAsync();
}
using System.Threading.Tasks;

namespace VoiceMeeterVolumeLink.Configuration;

public interface IConfigurationManager
{
    Task<RootConfiguration> GetConfigurationAsync();
    Task SaveConfigurationAsync();
}
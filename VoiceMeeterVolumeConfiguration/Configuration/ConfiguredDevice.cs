using System.Text.Json.Serialization;

namespace VoiceMeeterVolumeConfiguration.Configuration;

public class ConfiguredDevice
{
    public bool LinkVolume { get; set; }
    public bool Mute { get; set; }
    public string? DeviceName { get; set; } 
}
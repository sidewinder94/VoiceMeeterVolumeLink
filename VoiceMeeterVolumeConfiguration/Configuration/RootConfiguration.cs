using System.Collections.Generic;

namespace VoiceMeeterVolumeConfiguration.Configuration;

public class RootConfiguration
{
    public Dictionary<string, ConfiguredDevice> ConfiguredDevices { get; set; } = new();
}
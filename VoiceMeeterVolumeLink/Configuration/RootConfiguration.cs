using System.Collections.Generic;

namespace VoiceMeeterVolumeLink.Configuration;

public class RootConfiguration
{
    public Dictionary<string, ConfiguredDevice> ConfiguredDevices { get; set; } = new();
}
using System.Collections.Generic;

namespace VoiceMeeterVolumeLink.Configuration;

public class RootConfiguration
{
    public Dictionary<string, ConfiguredDevice> ConfiguredDevices { get; set; } = new();
    
    public int? Width { get; set; }
    
    public int? Height { get; set; }
}
using System.Collections.Generic;
using VoiceMeeter.NET.Enums;

namespace VoiceMeeterVolumeLink.Configuration;

public class RootConfiguration
{
    public VoiceMeeterType? VoiceMeeterType { get; set; }
    
    public Dictionary<string, ConfiguredDevice> ConfiguredDevices { get; set; } = new();
    
    public int? Width { get; set; }
    
    public int? Height { get; set; }
}
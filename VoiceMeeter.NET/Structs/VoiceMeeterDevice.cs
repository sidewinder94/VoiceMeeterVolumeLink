using System.Diagnostics;
using System.Runtime.CompilerServices;
using VoiceMeeter.NET.Enums;

namespace VoiceMeeter.NET.Structs;

[DebuggerDisplay("Device: {DeviceType}, Name: {Name}, HardwareId: {HardwareId}")]
public readonly struct VoiceMeeterDevice
{
    public DeviceType DeviceType { get; init; }
    public string Name { get; init; }
    public string HardwareId { get; init; }
}
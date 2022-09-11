using JetBrains.Annotations;
using VoiceMeeter.NET.Enums;
using VoiceMeeter.NET.Structs;

namespace VoiceMeeter.NET;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public interface IVoiceMeeterClient
{
    LoginResponse Status { get; }
    LoginResponse Login();
    bool Logout();
    void RunVoiceMeeter(VoiceMeeterType voiceMeeterType);
    VoiceMeeterType GetVoiceMeeterType();
    Version GetVoiceMeeterVersion();
    bool IsDirty();
    float GetFloatParameter(string paramName);
    string GetStringParameter(string paramName);
    VoiceMeeterConfiguration GetConfiguration(TimeSpan? refreshDelay = null);
    long GetOutputDeviceCount();
    VoiceMeeterDevice GetOutputDevice(long index);
}
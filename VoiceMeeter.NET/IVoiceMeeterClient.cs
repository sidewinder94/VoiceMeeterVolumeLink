using JetBrains.Annotations;
using VoiceMeeter.NET.Enums;
using VoiceMeeter.NET.Structs;

namespace VoiceMeeter.NET;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public interface IVoiceMeeterClient
{
    /// <summary>
    /// 
    /// </summary>
    LoginResponse Status { get; }
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    LoginResponse Login();
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    bool Logout();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="voiceMeeterType"></param>
    void RunVoiceMeeter(VoiceMeeterType voiceMeeterType);
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    VoiceMeeterType GetVoiceMeeterType();
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Version GetVoiceMeeterVersion();
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    bool IsDirty();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="paramName"></param>
    /// <returns></returns>
    float GetFloatParameter(string paramName);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="paramName"></param>
    /// <returns></returns>
    string GetStringParameter(string paramName);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="refreshDelay"></param>
    /// <returns></returns>
    VoiceMeeterConfiguration GetConfiguration(TimeSpan? refreshDelay = null);
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    long GetOutputDeviceCount();
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    VoiceMeeterDevice GetOutputDevice(long index);
}
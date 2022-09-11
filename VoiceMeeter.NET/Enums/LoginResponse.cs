namespace VoiceMeeter.NET.Enums;

public enum LoginResponse
{
    AlreadyLoggedIn = -2,
    NoClient = -1,
    Ok = 0,
    VoiceMeeterNotRunning = 1,
    LoggedOff
}
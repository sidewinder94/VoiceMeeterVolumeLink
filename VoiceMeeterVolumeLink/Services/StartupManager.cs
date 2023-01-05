using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using Microsoft.Win32;

namespace VoiceMeeterVolumeLink.Services;

public class StartupManager
{
    private static string GetApplicationName() => Assembly.GetEntryAssembly().GetName().Name;
    private static string GetApplicationLocation() => $"\"{Process.GetCurrentProcess().MainModule?.FileName}\"";
    
    public static void AddApplicationToCurrentUserStartup()
    {
        using var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        key?.SetValue(GetApplicationName(), GetApplicationLocation());
    }

    public static void RemoveApplicationFromCurrentUserStartup()
    {
        using var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        key?.DeleteValue(GetApplicationName(), false);
    }

    public static bool IsApplicationInCurrentUserStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
            return key?.GetValue(GetApplicationName()) is not null;
        }
        catch
        {
            return false;
        }
    }

    public static void AddApplicationToAllUserStartup()
    {
        using var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        key?.SetValue(GetApplicationName(), GetApplicationLocation());
    }
    
    public static bool IsApplicationInAllUsersStartup()
    {
        using var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
        return key?.GetValue(GetApplicationName()) is not null;
    }

    public static void RemoveApplicationFromAllUserStartup()
    {
        using var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        key?.DeleteValue(GetApplicationName(), false);
    }

    public static bool IsUserAdministrator()
    {
        //bool value to hold our return value
        bool isAdmin;
        try
        {
            //get the currently logged in user
            var user = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(user);
            isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch (UnauthorizedAccessException)
        {
            isAdmin = false;
        }
        catch (Exception)
        {
            isAdmin = false;
        }
        return isAdmin;
    }
}
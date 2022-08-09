using System.Buffers;
using System.Reflection;
using Castle.DynamicProxy;
using JetBrains.Annotations;
using VoiceMeeter.NET.Attributes;
using VoiceMeeter.NET.Enums;
using VoiceMeeter.NET.Exceptions;
using VoiceMeeter.NET.Extensions;
using VoiceMeeter.NET.Structs;

namespace VoiceMeeter.NET;

public class VoiceMeeterClient : IVoiceMeeterClient, IDisposable
{
    [UsedImplicitly] public LoginResponse Status { get; private set; } = LoginResponse.LoggedOff;

    private readonly object _lockObj = new();

    private VoiceMeeterClient()
    {
    }

    public static IVoiceMeeterClient Create()
    {
        var proxyGenerator = new ProxyGenerator();
        var client = new VoiceMeeterClient();
        var interceptor = new ClientInterceptor(client);
        return proxyGenerator.CreateInterfaceProxyWithTargetInterface<IVoiceMeeterClient>(client, interceptor);
    }

    public LoginResponse Login()
    {
        this.Status = NativeMethods.Login();

        return this.Status;
    }

    [AllowNotLaunched]
    public bool Logout()
    {
        long result = NativeMethods.Logout();

        if (result != 0) return false;

        this.Status = LoginResponse.LoggedOff;
        return true;
    }

    [AllowNotLaunched]
    public void RunVoiceMeeter(VoiceMeeterType voiceMeeterType)
    {
        long result = NativeMethods.RunVoiceMeeter((long)voiceMeeterType);

        switch (result)
        {
            case -1:
                throw new VoiceMeeterException("VoiceMeeter is not installed");
            case -2:
                throw new ArgumentOutOfRangeException(nameof(voiceMeeterType));
            default:
                break;
        }
    }
    
    public  VoiceMeeterConfiguration GetConfiguration(TimeSpan? refreshDelay = null)
    {
        return new VoiceMeeterConfiguration(this, refreshDelay, this.GetVoiceMeeterType()).Init();
    }

    public VoiceMeeterType GetVoiceMeeterType()
    {
        NativeMethods.GetVoiceMeeterType(out long result);

        return (VoiceMeeterType)result;
    }

    public Version GetVoiceMeeterVersion()
    {
        NativeMethods.GetVoiceMeeterVersion(out long version);

        return new Version(
            (int)((version & 0xFF000000) >> 24),
            (int)((version & 0x00FF0000) >> 16),
            (int)((version & 0x0000FF00) >> 8),
            (int)version & 0x000000FF);
    }

    public bool IsDirty()
    {
        lock (this._lockObj)
        {
            bool result = NativeMethods.IsParametersDirty() == 1;

            return result;
        }
    }

    public float GetFloatParameter(string paramName)
    {
        lock (this._lockObj)
        {
            long status = NativeMethods.GetParameter(paramName, out float result);

            this.AssertGetParamResult(status, paramName);

            return result;
        }
    }

    public string GetStringParameter(string paramName)
    {
        lock (this._lockObj)
        {
            char[] buffer = ArrayPool<char>.Shared.Rent(512 + 1);

            try
            {
                long status = NativeMethods.GetParameter(paramName, buffer);

                this.AssertGetParamResult(status, paramName);

                return buffer.GetStringFromNullTerminatedCharArray();
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer, true);
            }
        }
    }

    public long GetOutputDeviceCount()
    {
        return NativeMethods.GetOutputDeviceNumber();
    }

    public VoiceMeeterDevice GetOutputDevice(long index)
    {
        char[] deviceNameBuffer = ArrayPool<char>.Shared.Rent(512 + 1);
        char[] deviceHardwareIdBuffer = ArrayPool<char>.Shared.Rent(512 + 1);

        try
        {
            long status = NativeMethods.GetOutputDeviceDescription(index, out long type, deviceNameBuffer, deviceHardwareIdBuffer);

            return new VoiceMeeterDevice()
            {
                DeviceType = (DeviceType)type,
                Name = deviceNameBuffer.GetStringFromNullTerminatedCharArray(),
                HardwareId = deviceHardwareIdBuffer.GetStringFromNullTerminatedCharArray()
            };
        }
        finally
        {
            ArrayPool<char>.Shared.Return(deviceNameBuffer, true);
            ArrayPool<char>.Shared.Return(deviceHardwareIdBuffer, true);
        }
    }

    internal void SetParameter(string paramName, string value)
    {
        long status = NativeMethods.SetParameter(paramName, value);

        this.AssertSetParamResult(status, paramName);
    }

    internal void SetParameter(string paramName, float value)
    {
        long status = NativeMethods.SetParameter(paramName, value);

        this.AssertSetParamResult(status, paramName);
    }

    internal void SetParameters(string script)
    {
        long status = NativeMethods.SetParameters(script);

        switch (status)
        {
            case 0: break;
            case > 0: throw new VoiceMeeterScriptException($"Script error on line {status}", script);
            case -2: throw new VoiceMeeterNotLoggedException();
            default: throw new VoiceMeeterException("Unknown Error");
        }
    }

    private void AssertSetParamResult(long result, string paramName)
    {
        switch (result)
        {
            case 0: return;
            case -1: throw new VoiceMeeterException("Error");
            case -2: throw new VoiceMeeterNotLoggedException();
            case -3: throw new ArgumentOutOfRangeException(paramName);
            default: throw new VoiceMeeterException("Unknown Error");
        }
    }

    private void AssertGetParamResult(long result, string paramName)
    {
        switch (result)
        {
            case 0: return;
            case -1: throw new VoiceMeeterException("Error");
            case -2: throw new VoiceMeeterNotLoggedException();
            case -3: throw new ArgumentOutOfRangeException(paramName);
            case -5: throw new VoiceMeeterException("Structure Mismatch");
            default: throw new VoiceMeeterException("Unknown Error");
        }
    }

    private class ClientInterceptor : IInterceptor
    {
        private IVoiceMeeterClient Client { get; }

        public ClientInterceptor(IVoiceMeeterClient client)
        {
            this.Client = client;
        }

        private void AssertLoggedIn(bool allowNotLaunched = false)
        {
            // Allows execution to continue if Status is ok
            if (this.Client.Status == LoginResponse.Ok ||
                // Or if OkButNotLaunched allowed, allow that status too
                (allowNotLaunched && this.Client.Status == LoginResponse.VoiceMeeterNotRunning)) return;

            // Else throw
            throw new VoiceMeeterNotLoggedException();
        }

        public void Intercept(IInvocation invocation)
        {
            if (invocation.Method.Name != nameof(Login))
            {
                var allowNotLaunched =
                    invocation.MethodInvocationTarget.GetCustomAttribute(typeof(AllowNotLaunchedAttribute));

                this.AssertLoggedIn(allowNotLaunched != null);
            }

            invocation.Proceed();
        }
    }

    private void ReleaseUnmanagedResources()
    {
        this.Logout();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    ~VoiceMeeterClient()
    {
        ReleaseUnmanagedResources();
    }
}
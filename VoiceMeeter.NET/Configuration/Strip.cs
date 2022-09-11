using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using VoiceMeeter.NET.Attributes;
using VoiceMeeter.NET.Enums;

namespace VoiceMeeter.NET.Configuration;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class Strip : VoiceMeeterResource<Strip>
{
    private float _gain;

    private bool _mute;

    private string _deviceName = string.Empty;

    /// <inheritdoc/>
    public override string ResourceType => nameof(Strip);

    /// <summary>
    /// Returns a value indicating if this is a virtual <see cref="Strip"/>
    /// </summary>
    public bool IsVirtual => string.IsNullOrWhiteSpace(this.DeviceName);

    public string? VirtualDeviceName { get; internal set; }

    [Range(-60.0f, 12.0f)]
    [VoiceMeeterParameter(nameof(_gain), "Gain", ParamType.Float)]
    public virtual float Gain
    {
        get => this._gain;
        set => this.SetProperty(ref this._gain, value);
    }

    [VoiceMeeterParameter(nameof(_mute), "Mute", ParamType.Bool)]
    public virtual bool Mute
    {
        get => this._mute;
        set => this.SetProperty(ref this._mute, value);
    }

    [VoiceMeeterParameter(nameof(_deviceName), "device.name", ParamType.String, ParamMode = ParamMode.ReadOnly)]
    public string DeviceName
    {
        get => this._deviceName;
        internal set => this.SetProperty(ref this._deviceName, value);
    }

    [VoiceMeeterParameter(nameof(_deviceName), "device.wdm", ParamType.String, ParamMode = ParamMode.WriteOnly)]
    public string WdmDevice
    {
        internal get => this._deviceName;
        set => this.SetProperty(ref this._deviceName, value);
    }

    internal Strip(ChangeTracker changeTracker, VoiceMeeterType voiceMeeterType, int index) : base(changeTracker, voiceMeeterType, index)
    {
    }

    internal Strip Init()
    {
        return this;
    }
}
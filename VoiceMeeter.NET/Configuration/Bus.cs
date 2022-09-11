using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using VoiceMeeter.NET.Attributes;
using VoiceMeeter.NET.Enums;

namespace VoiceMeeter.NET.Configuration;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class Bus : VoiceMeeterResource<Bus>
{
    private bool _mono;
    private bool _mute;
    private bool _isEqEnabled;
    private float _gain;
    private string _deviceName = string.Empty;
    private bool _isEqBEnabled;

    /// <inheritdoc/>
    public override string ResourceType => nameof(Bus);

    /// <summary>
    /// Returns a value indicating if this is a virtual bus
    /// </summary>
    public virtual bool IsVirtual { get; internal set; }

    [VoiceMeeterParameter(nameof(_mono), "Mono", ParamType.Bool)]
    public bool Mono
    {
        get => this._mono;
        set => this.SetProperty(ref this._mono, value);
    }

    [VoiceMeeterParameter(nameof(_mute), "Mute", ParamType.Bool)]
    public bool Mute
    {
        get => this._mute;
        set => this.SetProperty(ref this._mute, value);
    }

    [VoiceMeeterParameter(nameof(_isEqEnabled), "EQ.on", ParamType.Bool)]
    public bool IsEqEnabled
    {
        get => this._isEqEnabled;
        set => this.SetProperty(ref this._isEqEnabled, value);
    }

    /// <summary>
    /// Gets or sets a value indicating which EQ memory slot is in use
    /// </summary>
    /// <remarks>A <c>false</c> value here means that EQ A is enabled (default)</remarks>
    [VoiceMeeterParameter(nameof(_isEqBEnabled), "EQ.AB", ParamType.Bool)]
    public bool IsEqBEnabled
    {
        get => this._isEqBEnabled;
        set => this.SetProperty(ref this._isEqBEnabled, value);
    }

    [Range(-60.0f, 12.0f)]
    [VoiceMeeterParameter(nameof(_gain),"Gain", ParamType.Float)]
    public float Gain
    {
        get => this._gain;
        set => this.SetProperty(ref this._gain, value);
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

    internal Bus(ChangeTracker changeTracker, VoiceMeeterType voiceMeeterType, int index) : base(changeTracker, voiceMeeterType, index)
    {
    }
    
    internal Bus Init()
    {
        return this;
    }
}
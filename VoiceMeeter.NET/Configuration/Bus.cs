﻿using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using VoiceMeeter.NET.Attributes;
using VoiceMeeter.NET.Enums;

namespace VoiceMeeter.NET.Configuration;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class Bus : VoiceMeeterResource<Bus>
{
    private float _gain;

    private bool _mute;

    private string _deviceName = string.Empty;

    /// <inheritdoc/>
    public override string ResourceType => nameof(Bus);

    /// <summary>
    /// Returns a value indicating if this is a virtual bus
    /// </summary>
    public virtual bool IsVirtual { get; internal set; }

    [Range(-60.0f, 12.0f)]
    [VoiceMeeterParameter(nameof(_gain),"Gain", ParamType.Float)]
    public float Gain
    {
        get => this._gain;
        set => this.SetProperty(ref this._gain, value);
    }

    [VoiceMeeterParameter(nameof(_mute), "Mute", ParamType.Bool)]
    public bool Mute
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

    internal Bus(ChangeTracker changeTracker, VoiceMeeterType voiceMeeterType, int index) : base(changeTracker, voiceMeeterType, index)
    {
    }
    
    internal Bus Init()
    {
        return this;
    }
}
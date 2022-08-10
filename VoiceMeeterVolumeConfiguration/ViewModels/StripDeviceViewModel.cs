using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive;
using NAudio.CoreAudioApi;
using VoiceMeeter.NET.Configuration;
using VoiceMeeterVolumeConfiguration.Configuration;
using VoiceMeeterVolumeConfiguration.Services;

namespace VoiceMeeterVolumeConfiguration.ViewModels;

public class StripDeviceViewModel : BaseDeviceViewModel
{
    private bool? _isMute;
    private readonly Strip _resource;

    public bool IsMute
    {
        get => this._isMute ?? false;
        set
        {
            if (!this.SetProperty(ref this._isMute, value)) return;
            if (this._resource.Mute == value) return;

            this._resource.Mute = value;

            this.PersistMute(value);
        }
    }

    /// <inheritdoc />
    public StripDeviceViewModel(Strip voiceMeeterResource, IConfigurationManager configurationManager) : base(
        voiceMeeterResource, configurationManager)
    {
        this._resource = voiceMeeterResource;

        this.VoiceMeeterName = voiceMeeterResource.VirtualDeviceName;

        this.AudioService = new AudioService((voiceMeeterResource.VirtualDeviceName ?? voiceMeeterResource.Name) ?? $"Strip - {voiceMeeterResource.Index}")
        {
            AvailableDeviceNames = this.AvailableDeviceNames
        };

        this.AudioService.PropertyChanged += this.OnDeviceIdUpdated;
        
        this.VolumeChangeSubscription = this.AudioService.Subscribe(this.AudioEndpointVolumeChanged);
        
        this.LinkDevice(this.AudioService);
    }

    /// <inheritdoc />
    protected override async void OnDeviceIdUpdated(object? sender, PropertyChangedEventArgs? args)
    {
        Dictionary<string, ConfiguredDevice> configuredDevices = (await this.Configuration).ConfiguredDevices;
        string? deviceId = this.AudioService?.CurrentDeviceId;
        
        if (deviceId == null ||
            !configuredDevices.ContainsKey(deviceId)) return;

        this.IsMute = configuredDevices[deviceId].Mute;
        this.LinkVolume = configuredDevices[deviceId].LinkVolume;
    }
    
    /// <summary>
    /// Called to find the associated virtual device
    /// </summary>
    private void LinkDevice(AudioService service)
    {
       service.UseDevice = this._resource.VirtualDeviceName;
    }

    /// <inheritdoc />
    protected override void OnValueUpdate(EventPattern<PropertyChangedEventArgs> obj)
    {
        string? propertyName = obj.EventArgs.PropertyName;

        if (string.IsNullOrWhiteSpace(propertyName)) return;

        object? value = typeof(Strip).GetProperty(propertyName)?.GetValue(this._resource);

        switch (propertyName)
        {
            case nameof(IVoiceMeeterResource.Name):
                this.VoiceMeeterName = this._resource.Name;
                break;
            case nameof(Strip.Gain):
                this.OnVoiceMeeterGainChange((float?)value);
                break;
            case nameof(Strip.Mute):
                this.OnVoiceMeeterMuteChange((bool?)value);
                break;
            default:
                return;
        }
    }

    /// <inheritdoc/>
    protected override void AudioEndpointVolumeChanged((AudioVolumeNotificationData volumeData, float volumeScalar) evt)
    {
        (var volumeData, float volumeScalar) = evt;
        
        if (!this.LinkVolume) return;

        this._resource.Mute = volumeData.Muted;

        float destinationGain = ProportionalGain(GetWindowsVolumeRange(), GetVoiceMeeterVolumeRange(),
            volumeScalar);

        this._resource.Gain = destinationGain;
    }

    private void OnVoiceMeeterMuteChange(bool? isMute)
    {
        this.SetProperty(ref this._isMute, isMute, nameof(this.IsMute));

        if (!isMute.HasValue) return;

        this.AudioService?.SetMute(isMute.Value);
    }
}
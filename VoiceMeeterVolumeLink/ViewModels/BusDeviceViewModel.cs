using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive;
using System.Windows.Media.Animation;
using NAudio.CoreAudioApi;
using VoiceMeeter.NET.Configuration;
using VoiceMeeterVolumeLink.Configuration;
using VoiceMeeterVolumeLink.Services;

namespace VoiceMeeterVolumeLink.ViewModels;

public class BusDeviceViewModel : BaseDeviceViewModel
{
    private string _voiceMeeterBus;
    private string? _associatedDeviceName;
    private bool? _isMute;
    private readonly Bus _resource;

    public string VoiceMeeterBus
    {
        get => this._voiceMeeterBus;
        set => this.SetProperty(ref this._voiceMeeterBus, value);
    }

    public string? AssociatedDeviceName
    {
        get => this._associatedDeviceName;
        set
        {
            if (!this.SetProperty(ref this._associatedDeviceName, value) || value == null) return;
            if (this._resource.DeviceName == value) return;

            this._resource.WdmDevice = value;
        }
    }

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

    public BusDeviceViewModel(string busName, Bus voiceMeeterResource, IConfigurationManager configurationManager) :
        base(voiceMeeterResource, configurationManager)
    {
        this._resource = voiceMeeterResource;
        this._voiceMeeterBus = busName;

        this.AudioService = new AudioService($"{busName} {voiceMeeterResource.Name}")
        {
            AvailableDeviceNames = this.AvailableDeviceNames
        };

        this.AudioService.PropertyChanged += this.OnDeviceIdUpdated;

        this.VolumeChangeSubscription = this.AudioService.Subscribe(this.AudioEndpointVolumeChanged);

        this.OnVoiceMeeterDeviceChanged(this._resource.DeviceName);
        this.AvailableDeviceNames.CollectionChanged += this.AvailableDeviceNamesOnCollectionChanged;
    }

    private void AvailableDeviceNamesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.SetProperty(ref this._associatedDeviceName, this._resource.DeviceName, nameof(this.AssociatedDeviceName));
    }

    /// <inheritdoc />
    protected override async void OnDeviceIdUpdated(object? sender, PropertyChangedEventArgs? args)
    {
        Dictionary<string, ConfiguredDevice> configuredDevices = (await this.Configuration).ConfiguredDevices;
        string? deviceId = this.AudioService?.CurrentDeviceId;

        if (deviceId == null ||
            !configuredDevices.ContainsKey(deviceId))
        {
            this.IsMute = false;
            this.LinkVolume = false;
            return;
        }

        this.IsMute = configuredDevices[deviceId].Mute;
        this.LinkVolume = configuredDevices[deviceId].LinkVolume;
    }

    protected override void OnValueUpdate(EventPattern<PropertyChangedEventArgs> obj)
    {
        string? propertyName = obj.EventArgs.PropertyName;

        if (string.IsNullOrWhiteSpace(propertyName)) return;

        object? value = typeof(Bus).GetProperty(propertyName)?.GetValue(this._resource);

        switch (propertyName)
        {
            case nameof(IVoiceMeeterResource.Name):
                this.VoiceMeeterName = this._resource.Name;
                break;
            case nameof(Bus.DeviceName):
                this.OnVoiceMeeterDeviceChanged((string?)value);
                break;
            case nameof(Bus.Gain):
                this.OnVoiceMeeterGainChange((float?)value);
                break;
            case nameof(Bus.Mute):
                this.OnVoiceMeeterMuteChange((bool?)value);
                break;
            default:
                return;
        }
    }

    /// <inheritdoc/>
    protected override void AudioEndpointVolumeChanged((AudioVolumeNotificationData volumeData, float volumeScalar) evt)
    {
        if (!this.CanUpdateVolume(isVoiceMeeterVolume: false)) return;
        this.TakeVolumeLead(isVoiceMeeterVolume: false);
        
        (var volumeData, float volumeScalar) = evt;

        if (!this.LinkVolume) return;

        this._resource.Mute = volumeData.Muted;

        float proportionalGain = ProportionalGain(GetWindowsVolumeRange(), GetVoiceMeeterVolumeRange(), volumeScalar);
        
        proportionalGain = Math.Abs(proportionalGain) <= 0.3f ? 0.0f : proportionalGain;

        this._resource.Gain = proportionalGain;
    }

    public void RefreshDeviceList()
    {
        this.AudioService?.RefreshAvailableDevices();
    }

    /// <summary>
    /// Called when the associated device was changed on VoiceMeeter side
    /// </summary>
    /// <param name="name">The name of the new device</param>
    private void OnVoiceMeeterDeviceChanged(string? name)
    {
        if (this.AudioService == null) return;

        this.AudioService.UseDevice = name;

        // And set the name for the GUI, we directly use SetProperty, because if VM clears the value we don't want to retain it on the GUI
        this.SetProperty(ref this._associatedDeviceName, name, nameof(this.AssociatedDeviceName));
    }

    private void OnVoiceMeeterMuteChange(bool? isMute)
    {
        this.SetProperty(ref this._isMute, isMute, nameof(this.IsMute));

        if (!isMute.HasValue) return;

        this.AudioService?.SetMute(isMute.Value);
    }
}
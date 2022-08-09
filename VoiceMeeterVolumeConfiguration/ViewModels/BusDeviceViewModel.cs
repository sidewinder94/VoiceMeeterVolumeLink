using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Threading;
using NAudio.CoreAudioApi;
using VoiceMeeter.NET.Configuration;
using VoiceMeeterVolumeConfiguration.Configuration;

namespace VoiceMeeterVolumeConfiguration.ViewModels;

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


        this.OnVoiceMeeterDeviceChanged(this._resource.DeviceName);

        this.RefreshDeviceList();
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
    protected override void AudioEndpointVolumeChanged(AudioVolumeNotificationData volumeData)
    {
        if (!this.LinkVolume) return;

        this._resource.Mute = volumeData.Muted;

        if (this.SelectedDevice == null) return;

        lock (this.SelectedDevice)
        {
            float destinationGain = ProportionalGain(GetWindowsVolumeRange(), GetVoiceMeeterVolumeRange(),
                this.SelectedDevice.AudioEndpointVolume.MasterVolumeLevelScalar);
            this._resource.Gain = destinationGain;
        }
    }

    public void RefreshDeviceList()
    {
        var selectedDevice = this.AssociatedDeviceName;

        var enumerator = new MMDeviceEnumerator();

        this.OnVoiceMeeterDeviceChanged(null);

        this.DeviceLookup = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .Select(GetDeviceIdentification)
            .ToLookup(d => d.FriendlyName, v => v.Id);
        this.AvailableDeviceNames.Clear();
        this.DeviceLookup.Select(g => g.Key).ForEach(this.AvailableDeviceNames.Add);

        this.OnVoiceMeeterDeviceChanged(selectedDevice);
    }

    /// <summary>
    /// Called when the associated device was changed on VoiceMeeter side
    /// </summary>
    /// <param name="name">The name of the new device</param>
    private void OnVoiceMeeterDeviceChanged(string? name)
    {
        // We unsubscribe from notification from the old device and null it
        if (this.SelectedDevice != null)
        {
            lock (this.SelectedDevice)
            {
                this.SelectedDevice.AudioEndpointVolume.OnVolumeNotification -= this.AudioEndpointVolumeChanged;
                this.SelectedDevice.Dispose();
                this.SelectedDevice = null;
            }
        }

        // And set the name for the GUI, we directly use SetProperty, because if VM clears the value we don't want to retain it on the GUI
        this.SetProperty(ref this._associatedDeviceName, name, nameof(this.AssociatedDeviceName));

        // If we don't know the new device, we do nothing more
        if (name == null
            || this.DeviceLookup == null
            || !this.DeviceLookup.Contains(name)
            || !this.DeviceLookup[name].Any()) return;


        // If we do, we subscribe to volume change notifications
        var success = false;

        do
        {
            try
            {
                this.SelectedDevice = new MMDeviceEnumerator().GetDevice(this.DeviceLookup[name].First());

                lock (this.SelectedDevice)
                {
                    this.SelectedDevice.AudioEndpointVolume.OnVolumeNotification += this.AudioEndpointVolumeChanged;

                    success = true;

                    // And load the config if it exists
                    this.SelectedDeviceId = this.SelectedDevice.ID;
                }

                var configuration = this.Configuration.Value.Result;

                if (!configuration.ConfiguredDevices.ContainsKey(SelectedDeviceId))
                {
                    configuration.ConfiguredDevices[this.SelectedDeviceId] = new ConfiguredDevice
                    {
                        DeviceName = name
                    };
                }
                
                this.IsMute = configuration.ConfiguredDevices[this.SelectedDeviceId].Mute;
                this.LinkVolume = configuration.ConfiguredDevices[this.SelectedDeviceId].LinkVolume;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        } while (!success);
    }

    private void OnVoiceMeeterMuteChange(bool? isMute)
    {
        this.SetProperty(ref this._isMute, isMute, nameof(this.IsMute));

        if (!isMute.HasValue || this.SelectedDevice == null) return;
        
        lock (this.SelectedDevice)
        {
            this.SelectedDevice.AudioEndpointVolume.Mute = isMute.Value;    
        }
    }
}
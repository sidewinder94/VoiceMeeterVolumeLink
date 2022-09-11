using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using NAudio.CoreAudioApi;
using VoiceMeeter.NET.Configuration;
using VoiceMeeterVolumeConfiguration.Configuration;

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
    public StripDeviceViewModel(Strip voiceMeeterResource, IConfigurationManager configurationManager) : base(voiceMeeterResource, configurationManager)
    {
        this._resource = voiceMeeterResource;

        this.VoiceMeeterName = voiceMeeterResource.VirtualDeviceName;
        
        this.LinkDevice();
    }

    /// <summary>
    /// Called to find the associated virtual device
    /// </summary>

    private void LinkDevice()
    {
        var enumerator = new MMDeviceEnumerator();

        this.DeviceLookup = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .Select(GetDeviceIdentification)
            .ToLookup(d => d.FriendlyName, v => v.Id);

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

        // If we don't know the new device, we do nothing more
        if (this._resource.VirtualDeviceName == null
            || this.DeviceLookup == null
            || !this.DeviceLookup.Contains(this._resource.VirtualDeviceName)
            || !this.DeviceLookup[this._resource.VirtualDeviceName].Any()) return;

        // If we do, we subscribe to volume change notifications
        var success = false;

        do
        {
            try
            {
                this.SelectedDevice = new MMDeviceEnumerator().GetDevice(this.DeviceLookup[this._resource.VirtualDeviceName].First());

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
                        DeviceName = this._resource.VirtualDeviceName
                    };
                };

                this.IsMute = configuration.ConfiguredDevices[this.SelectedDeviceId].Mute;
                this.LinkVolume = configuration.ConfiguredDevices[this.SelectedDeviceId].LinkVolume;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        } while (!success);
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
    protected override void AudioEndpointVolumeChanged(AudioVolumeNotificationData volumeData)
    {
        if (!this.LinkVolume) return;

        this._resource.Mute = volumeData.Muted;

        if (this.SelectedDevice == null) return;

        lock (this.SelectedDevice)
        {
            float destinationGain = ProportionalGain(GetWindowsVolumeRange(), GetVoiceMeeterVolumeRange(),
                this.SelectedDevice!.AudioEndpointVolume.MasterVolumeLevelScalar);

            this._resource.Gain = destinationGain;
        }
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
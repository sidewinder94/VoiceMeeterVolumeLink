﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reactive;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.CoreAudioApi;
using VoiceMeeter.NET.Configuration;
using VoiceMeeterVolumeConfiguration.Async;
using VoiceMeeterVolumeConfiguration.Configuration;
using VoiceMeeterVolumeConfiguration.Services;


namespace VoiceMeeterVolumeConfiguration.ViewModels;

public abstract class BaseDeviceViewModel : ObservableObject
{
    private readonly IDisposable _updateSubscription;
    private bool _linkVolume;
    private int _index;
    private string? _voiceMeeterName;
    protected readonly IConfigurationManager ConfigurationManager;
    protected readonly AsyncLazy<RootConfiguration> Configuration;
    protected AudioService? AudioService;
    protected IDisposable? VolumeChangeSubscription;

    public int Index
    {
        get => this._index;
        set => this.SetProperty(ref this._index, value);
    }

    public string? VoiceMeeterName
    {
        get => this._voiceMeeterName;
        set => this.SetProperty(ref this._voiceMeeterName, value);
    }

    public ObservableCollection<string> AvailableDeviceNames { get; } = new();

    public bool LinkVolume
    {
        get => this._linkVolume;
        set
        {
            this.SetProperty(ref this._linkVolume, value);

            this.PersistLinkVolume(value);
        }
    }

    ~BaseDeviceViewModel()
    {
        this._updateSubscription?.Dispose();
        this.VolumeChangeSubscription?.Dispose();
    }

    protected BaseDeviceViewModel(IVoiceMeeterResource voiceMeeterResource, IConfigurationManager configurationManager)
    {
        this.ConfigurationManager = configurationManager;
        this.Configuration = new AsyncLazy<RootConfiguration>(this.ConfigurationManager.GetConfigurationAsync);

        this._index = voiceMeeterResource.Index;
        this._voiceMeeterName = voiceMeeterResource.Name;

        this._updateSubscription = voiceMeeterResource.PropertyChangedObservable.Subscribe(this.OnValueUpdate);
    }

    protected abstract void OnDeviceIdUpdated(object? sender, PropertyChangedEventArgs? args);
    
    protected abstract void OnValueUpdate(EventPattern<PropertyChangedEventArgs> obj);

    /// <summary>
    /// Called when the volume changed from Windows' side <br/>
    /// Covers volume changes &amp; mute
    /// </summary>
    /// <param name="evt">The event containing the new volume data and the volume scalar (volume on a scale of 0..1)</param>
    protected abstract void AudioEndpointVolumeChanged((AudioVolumeNotificationData volumeData, float volumeScalar) evt);

    protected static (string FriendlyName, string Id) GetDeviceIdentification(MMDevice device)
    {
        var result = (device.FriendlyName, device.ID);

        // We are not going to use this device instance after that, we clean up after ourselves
        device.Dispose();

        return result;
    }

    protected void OnVoiceMeeterGainChange(float? gain)
    {
        if (!gain.HasValue || !this._linkVolume) return;

        this.AudioService?.SetVolume(
            ProportionalGain(GetVoiceMeeterVolumeRange(), GetWindowsVolumeRange(), gain.Value));
    }

    protected static (float MinVolume, float MaxVolume) GetVoiceMeeterVolumeRange()
    {
        var range = typeof(Bus).GetProperty(nameof(Bus.Gain))!.GetCustomAttribute<RangeAttribute>()!;

        // Double cast, because stored as a double in the attribute
        return ((float)(double)range.Minimum, (float)(double)range.Maximum);
    }

    protected static (float MinVolume, float MaxVolume) GetWindowsVolumeRange()
    {
        // Some devices return bogus data, we'll work only with percentages instead
        //var range = this._selectedDevice!.AudioEndpointVolume.VolumeRange;

        //return (range.MinDecibels, range.MaxDecibels);

        return (0f, 1f);
    }

    protected static float ProportionalGain((float MinVolume, float MaxVolume) source,
        (float MinVolume, float MaxVolume) destination, float value)
    {
        if (value > source.MaxVolume) value = source.MaxVolume;

        float slope = (destination.MaxVolume - destination.MinVolume) / (source.MaxVolume - source.MinVolume);
        float output = destination.MinVolume + slope * (value - source.MinVolume);

        return output; // Might have to round
    }

    private async void PersistLinkVolume(bool value)
    {
        if (this.AudioService?.CurrentDeviceId == null ||
            !(await this.Configuration).ConfiguredDevices.ContainsKey(this.AudioService.CurrentDeviceId)) return;
        (await this.Configuration).ConfiguredDevices[this.AudioService.CurrentDeviceId].LinkVolume = value;

        await this.ConfigurationManager.SaveConfigurationAsync();
    }

    protected async void PersistMute(bool value)
    {
        if (this.AudioService?.CurrentDeviceId == null ||
            !(await this.Configuration).ConfiguredDevices.ContainsKey(this.AudioService.CurrentDeviceId)) return;
        (await this.Configuration).ConfiguredDevices[this.AudioService.CurrentDeviceId].Mute = value;

        await this.ConfigurationManager.SaveConfigurationAsync();
    }
}
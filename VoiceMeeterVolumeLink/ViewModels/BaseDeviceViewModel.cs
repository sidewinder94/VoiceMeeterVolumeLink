using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reactive;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.CoreAudioApi;
using VoiceMeeter.NET.Configuration;
using VoiceMeeterVolumeLink.Async;
using VoiceMeeterVolumeLink.Configuration;
using VoiceMeeterVolumeLink.Services;

namespace VoiceMeeterVolumeLink.ViewModels;

public abstract class BaseDeviceViewModel : ObservableObject, IDisposable
{
    private readonly IDisposable _updateSubscription;
    private readonly IConfigurationManager _configurationManager;
    private readonly ManualResetEvent _audioLinkSync = new(true);
    private readonly Timer _syncResetTimer;
    private bool _linkVolume;
    private int _index;
    private string? _voiceMeeterName;

    protected readonly AsyncLazy<RootConfiguration> Configuration;
    protected AudioService? AudioService;
    protected IDisposable? VolumeChangeSubscription;
    private bool? _isVoiceMeeterMasterVolume = null;

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
        this.Dispose(false);
    }

    protected BaseDeviceViewModel(IVoiceMeeterResource voiceMeeterResource, IConfigurationManager configurationManager)
    {
        this._syncResetTimer = new Timer(ResetSync, this, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

        this._configurationManager = configurationManager;
        this.Configuration = new AsyncLazy<RootConfiguration>(this._configurationManager.GetConfigurationAsync);

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
    
    protected void TakeVolumeLead(bool isVoiceMeeterVolume)
    {
        this._syncResetTimer.Change(TimeSpan.FromMilliseconds(200), Timeout.InfiniteTimeSpan);
        this._isVoiceMeeterMasterVolume = isVoiceMeeterVolume;
        this._audioLinkSync.Reset();
    }
    
    protected bool CanUpdateVolume(bool isVoiceMeeterVolume)
    {
        return this._audioLinkSync.WaitOne(1) || this._isVoiceMeeterMasterVolume == isVoiceMeeterVolume;
    }
    
    protected void OnVoiceMeeterGainChange(float? gain)
    {
        if (!gain.HasValue || !this._linkVolume) return;
        
        if (!this.CanUpdateVolume(isVoiceMeeterVolume: true)) return;
        this.TakeVolumeLead(isVoiceMeeterVolume: true);

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

    private static void ResetSync(object? state)
    {
        if (state is not BaseDeviceViewModel viewModel) return;
        
        viewModel._audioLinkSync.Set();
    }

    private async void PersistLinkVolume(bool value)
    {
        if (this.AudioService?.CurrentDeviceId == null) return;

        var configuration = await this.Configuration;

        if (!configuration.ConfiguredDevices.ContainsKey(this.AudioService.CurrentDeviceId))
        {
            configuration.ConfiguredDevices.Add(this.AudioService.CurrentDeviceId, new ConfiguredDevice
            {
                DeviceName = this.AudioService.UseDevice
            });
        }
        
        configuration.ConfiguredDevices[this.AudioService.CurrentDeviceId].LinkVolume = value;

        await this._configurationManager.SaveConfigurationAsync();
    }

    protected async void PersistMute(bool value)
    {
        if (this.AudioService?.CurrentDeviceId == null) return;
        
        var configuration = await this.Configuration;

        if (!configuration.ConfiguredDevices.ContainsKey(this.AudioService.CurrentDeviceId))
        {
            configuration.ConfiguredDevices.Add(this.AudioService.CurrentDeviceId, new ConfiguredDevice
            {
                DeviceName = this.AudioService.UseDevice
            });
        }
        
        configuration.ConfiguredDevices[this.AudioService.CurrentDeviceId].Mute = value;

        await this._configurationManager.SaveConfigurationAsync();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        this._updateSubscription.Dispose();
        this.AudioService?.Dispose();
        this.VolumeChangeSubscription?.Dispose();
        this._audioLinkSync.Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }
}
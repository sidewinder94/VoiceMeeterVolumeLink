using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reactive;
using System.Windows;
using NAudio.CoreAudioApi;

namespace VoiceMeeterVolumeLink.Services;

public sealed class AudioService : ObservableBase<(AudioVolumeNotificationData volumeData, float volumeScalar)>,
    IDisposable, INotifyPropertyChanged
{
    private const string BaseThreadName = "NAudio";

    private readonly CancellationTokenSource _cts;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable // Want to conserve an explicit reference
    private readonly Thread _audioThread;
    private readonly ConcurrentQueue<(float? VolumeScalar, bool? mute)> _volumeChanges = new();
    private IObserver<(AudioVolumeNotificationData volumeData, float volumeScalar)>? _observer;
    private MMDevice? _currentDevice;
    private volatile bool _refreshAvailableDevices = true;
    private volatile string? _useDevice;
    private volatile ObservableCollection<string>? _availableDeviceNames;
    private volatile string? _currentDeviceId;

    public ObservableCollection<string>? AvailableDeviceNames
    {
        get => this._availableDeviceNames;
        init => this._availableDeviceNames = value;
    }

    public string? UseDevice
    {
        get => this._useDevice;
        set => this._useDevice = value;
    }

    public string? CurrentDeviceId
    {
        get => this._currentDeviceId;
        private set => this._currentDeviceId = value;
    }

    public AudioService(string resourceName)
    {
        this._cts = new CancellationTokenSource();

        this._audioThread = new Thread(this.Run)
        {
            Name = $"{BaseThreadName} - {resourceName}",
            CurrentCulture = CultureInfo.InvariantCulture,
            Priority = ThreadPriority.AboveNormal,
            IsBackground = true
        };

        //this._audioThread.DisableComObjectEagerCleanup();
        //Marshal.CleanupUnusedObjectsInCurrentContext();
        this._audioThread.SetApartmentState(ApartmentState.MTA);

        this._audioThread.Start(this._cts.Token);
    }

    public void SetVolume(float volumeScalar)
    {
        this._volumeChanges.Enqueue((volumeScalar, null));
    }

    public void SetMute(bool isMute)
    {
        this._volumeChanges.Enqueue((null, isMute));
    }

    public void RefreshAvailableDevices()
    {
        this._refreshAvailableDevices = true;
    }

    private void Run(object? obj)
    {
        if (obj is not CancellationToken ct) return;

        var enumerator = new MMDeviceEnumerator();

        ILookup<string, MMDevice> lookup = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .ToLookup(d => d.FriendlyName);

        string? selectedDevice = null;

        do
        {
            // Refresh available Devices
            if (this._refreshAvailableDevices)
            {
                lookup = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
                    .ToLookup(d => d.FriendlyName);

                if (this.AvailableDeviceNames != null)
                {
                    ILookup<string, MMDevice> lookupClosure = lookup;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        this.AvailableDeviceNames.Clear();
                        lookupClosure.Select(group => group.Key).ForEach(this.AvailableDeviceNames.Add);
                    });

                    this._refreshAvailableDevices = false;
                }
            }

            // If we changed the used device we unsubscribe from volume notifications, and go register on the new one
            if (selectedDevice != this.UseDevice)
            {
                if (this._currentDevice != null)
                {
                    this._currentDevice.AudioEndpointVolume.OnVolumeNotification -=
                        this.AudioEndpointVolumeOnVolumeNotification;
                }

                this._currentDevice = null;
                this.CurrentDeviceId = null;

                if (this.UseDevice != null && lookup.Contains(this.UseDevice))
                {
                    selectedDevice = this.UseDevice;

                    this._currentDevice = lookup[selectedDevice].First();
                    this.CurrentDeviceId = this._currentDevice.ID;

                    Application.Current.Dispatcher.Invoke(() =>
                        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.CurrentDeviceId))));

                    this._currentDevice.AudioEndpointVolume.OnVolumeNotification +=
                        this.AudioEndpointVolumeOnVolumeNotification;
                }

                if (this.UseDevice == null || !lookup.Contains(this.UseDevice))
                {
                    this.UseDevice = null;
                }
            }
            
            // Make Volume Changes
            while (this._volumeChanges.TryDequeue(out (float? VolumeScalar, bool? mute) changeNotification))
            {
                if (this._currentDevice == null) continue;

                (float? volume, bool? mute) = changeNotification;

                if (mute != null)
                {
                    this._currentDevice.AudioEndpointVolume.Mute = mute.Value;
                }

                if (volume != null)
                {
                    this._currentDevice.AudioEndpointVolume.MasterVolumeLevelScalar = volume.Value;
                }
            }

            Task.Delay(100).Wait();
        } while (!ct.IsCancellationRequested);

        this._observer?.OnCompleted();
    }

    private void AudioEndpointVolumeOnVolumeNotification(AudioVolumeNotificationData data)
    {
        if (this._currentDevice == null) return;

        this._observer?.OnNext((data, this._currentDevice.AudioEndpointVolume.MasterVolumeLevelScalar));
    }

    protected override IDisposable SubscribeCore(
        IObserver<(AudioVolumeNotificationData volumeData, float volumeScalar)> observer)
    {
        this._observer = observer;
        return new UnSubscriber(this, observer);
    }

    private class UnSubscriber : IDisposable
    {
        private readonly IObserver<(AudioVolumeNotificationData volumeData, float volumeScalar)> _observer;
        private readonly AudioService _audioService;

        public UnSubscriber(AudioService audioService,
            IObserver<(AudioVolumeNotificationData volumeData, float volumeScalar)> observer)
        {
            this._audioService = audioService;
            this._observer = observer;
        }

        public void Dispose()
        {
            if (!ReferenceEquals(this._audioService._observer, this._observer)) return;

            this._audioService._observer = null;
        }
    }

    public void Dispose()
    {
        this._cts.Cancel();
        this._cts.Dispose();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
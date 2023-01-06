using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceMeeter.NET;
using VoiceMeeter.NET.Enums;
using VoiceMeeterVolumeLink.Configuration;
using VoiceMeeterVolumeLink.Services;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace VoiceMeeterVolumeLink.ViewModels;

public class MainWindowViewModel : ObservableObject
{
    private readonly IVoiceMeeterClient _client;
    private readonly IConfigurationManager _configurationManager;
    private WindowState _windowState = WindowState.Minimized;
    private bool _showInTaskbar = true;
    private int _height = 250;
    private int _width = 800;
    private bool _isStartingWithWindows;

    private VoiceMeeterConfiguration? Configuration { get; set; }

    public WindowState WindowState
    {
        get => this._windowState;
        set
        {
            this.ShowInTaskbar = true;
            this.SetProperty(ref this._windowState, value);
            this.ShowInTaskbar = value != WindowState.Minimized;
            if (value != WindowState.Minimized) this.RefreshDeviceList();
        }
    }

    public bool ShowInTaskbar
    {
        get => this._showInTaskbar;
        set => this.SetProperty(ref this._showInTaskbar, value);
    }

    public int Height
    {
        get => this._height;
        set
        {
            if (!this.SetProperty(ref this._height, value)) return;
            this.SaveSize();
        }
    }

    public int Width
    {
        get => this._width;
        set
        {
            if (!this.SetProperty(ref this._width, value)) return;
            this.SaveSize();
        }
    }

    public bool IsStartingWithWindows
    {
        get => this._isStartingWithWindows;
        set
        {
            if (value == this._isStartingWithWindows) return;
            if (!HandleAutoStart(value)) return;
            this.SetProperty(ref this._isStartingWithWindows, value);
        }
    }

    public ObservableCollection<BusDeviceViewModel> Buses { get; } = new();

    public ObservableCollection<StripDeviceViewModel> Strips { get; } = new();

    public ICommand HideAndShowCommand { get; set; }

    public ICommand ClosingCommand { get; set; }

    public ICommand ExitCommand { get; set; }

    public MainWindowViewModel(IVoiceMeeterClient client, IConfigurationManager configurationManager)
    {
        this._isStartingWithWindows = StartupManager.IsApplicationInCurrentUserStartup();
        
        this._client = client;
        this._configurationManager = configurationManager;
        this.HideAndShowCommand = new RelayCommand<MouseEventArgs>(this.Click);
        this.ClosingCommand = new RelayCommand<MouseEventArgs>(this.Closing);
        this.ExitCommand = new RelayCommand<EventArgs>(this.Exit);

        this.InitAsync();
    }

    private async void InitAsync()
    {
        var settings = await this._configurationManager.GetConfigurationAsync();

        try
        {
            var status = this._client.Login();

            switch (status)
            {
                case LoginResponse.Ok when !settings.VoiceMeeterType.HasValue:
                    settings.VoiceMeeterType = this._client.GetVoiceMeeterType();
                    await this._configurationManager.SaveConfigurationAsync();
                    break;
                case LoginResponse.Ok:
                    break;
                case LoginResponse.VoiceMeeterNotRunning when settings.VoiceMeeterType.HasValue:
                    await this._client.RunAndWaitForVoiceMeeterAsync(settings.VoiceMeeterType.Value);
                    break;
                case LoginResponse.VoiceMeeterNotRunning:
                    MessageBox.Show("¨Please start VoiceMeeter before the first startup of this application", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                default:
                    MessageBox.Show("Error connecting to VoiceMeeter, please make sure it is installed properly", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    throw new ApplicationException();
            }
        }
        catch (DllNotFoundException e)
        {
            MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
        
        this.Configuration = await this._client.GetConfigurationAsync(TimeSpan.FromMilliseconds(150));

        await Task.Delay(150);

        this.Configuration.Buses
            .Where(kv => !kv.Value.IsVirtual)
            .Select(kv => new BusDeviceViewModel(kv.Key, kv.Value, this._configurationManager))
            .ForEach(this.Buses.Add);

        this.Configuration.Strips
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value.VirtualDeviceName))
            .Select(kv => new StripDeviceViewModel(kv.Value, this._configurationManager))
            .ForEach(this.Strips.Add);
    }

    private void Closing(MouseEventArgs? obj)
    {
        this.Configuration?.Dispose();
    }

    private void RefreshDeviceList()
    {
        this.Buses.ForEach(bus => bus.RefreshDeviceList());
    }

    private async void SaveSize()
    {
        var configuration = await this._configurationManager.GetConfigurationAsync();
        configuration.Height = this.Height;
        configuration.Width = this.Width;

        await this._configurationManager.SaveConfigurationAsync();
    }

    private async void Click(MouseEventArgs? obj)
    {
        if (obj is not { Button: MouseButtons.Left }) return;

        await this._configurationManager.GetConfigurationAsync().ContinueWith(async (task, state) =>
        {
            if (state is not MainWindowViewModel viewModel) return;

            var configuration = await task;

            if (configuration.Height.HasValue)
            {
                Application.Current.Dispatcher.Invoke(
                    () => viewModel.SetProperty(ref viewModel._height, configuration.Height.Value,
                        nameof(viewModel.Height)));
            }

            if (configuration.Width.HasValue)
            {
                Application.Current.Dispatcher.Invoke(
                    () => viewModel.SetProperty(ref viewModel._width, configuration.Width.Value,
                        nameof(viewModel.Width)));
            }
        }, this);

        this.WindowState = this.WindowState == WindowState.Minimized ? WindowState.Normal : WindowState.Minimized;
    }

    private static bool HandleAutoStart(bool startup)
    {
        try
        {
            if (startup)
            {
                return StartupManager.AddApplicationToCurrentUserStartup();
            }
            
            return StartupManager.RemoveApplicationFromCurrentUserStartup();
        }
        catch
        {
            return false;
        }
    }

    private void Exit(EventArgs? obj)
    {
        this._client.Logout();
        Application.Current.Shutdown();
    }
}
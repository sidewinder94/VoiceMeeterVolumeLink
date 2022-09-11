using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VoiceMeeter.NET;
using VoiceMeeterVolumeLink.Configuration;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace VoiceMeeterVolumeLink.ViewModels;

public class MainWindowViewModel : ObservableObject
{
    private readonly IVoiceMeeterClient _client;
    private WindowState _windowState = WindowState.Normal;
    private bool _showInTaskbar = true;

    private VoiceMeeterConfiguration Configuration { get; set; }

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

    public ObservableCollection<BusDeviceViewModel> Buses { get; } = new();
    public ObservableCollection<StripDeviceViewModel> Strips { get; } = new();
    public ICommand HideAndShowCommand { get; set; }
    public ICommand LoadedCommand { get; set; }
    public ICommand ClosingCommand { get; set; }
    public ICommand ExitCommand { get; set; }

    public MainWindowViewModel(IVoiceMeeterClient client, IConfigurationManager configurationManager)
    {
        this._client = client;
        this.HideAndShowCommand = new RelayCommand<MouseEventArgs>(this.Click);
        this.LoadedCommand = new RelayCommand<MouseEventArgs>(this.Loaded);
        this.ClosingCommand = new RelayCommand<MouseEventArgs>(this.Closing);
        this.ExitCommand = new RelayCommand<EventArgs>(this.Exit);

        try
        {
            client.Login();
        }
        catch (DllNotFoundException e)
        {
            MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        
        
        this.Configuration = client.GetConfiguration(TimeSpan.FromMilliseconds(150));

        Task.Delay(150).Wait();
        
        this.Configuration.Buses
            .Where(kv => !kv.Value.IsVirtual)
            .Select(kv => new BusDeviceViewModel(kv.Key, kv.Value, configurationManager))
            .ForEach(this.Buses.Add);
        
        this.Configuration.Strips
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value.VirtualDeviceName))
            .Select(kv => new StripDeviceViewModel(kv.Value, configurationManager))
            .ForEach(this.Strips.Add);
    }

    private void Closing(MouseEventArgs? obj)
    {
        this.Configuration.Dispose();
    }

    private void RefreshDeviceList()
    {
        this.Buses.ForEach(bus => bus.RefreshDeviceList());
    }
    
    private void Click(MouseEventArgs? obj)
    {
        if (obj == null) return;
        
        if (obj.Button == MouseButtons.Left)
        {
            this.WindowState = this.WindowState == WindowState.Minimized ? WindowState.Normal : WindowState.Minimized;
        }
    }

    private void Exit(EventArgs? obj)
    {
        this._client.Logout();
        Application.Current.Shutdown();
    }

    private void Loaded(MouseEventArgs? obj)
    {
       this.WindowState = WindowState.Minimized;
    }
}
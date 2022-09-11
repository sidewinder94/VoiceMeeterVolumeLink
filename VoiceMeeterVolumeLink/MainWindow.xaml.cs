using System;
using System.ComponentModel;
using System.Windows;
using VoiceMeeterVolumeLink.ViewModels;

namespace VoiceMeeterVolumeLink;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        this.InitializeComponent();
        this.Hide();

        if (this.DataContext is INotifyPropertyChanging dataContext)
        {
            dataContext.PropertyChanging += (sender, args) =>
            {
                if (sender is not MainWindowViewModel viewModel ||
                    args.PropertyName != nameof(MainWindowViewModel.WindowState)) return;
                
                if (viewModel.WindowState != WindowState.Minimized)
                {
                    this.Hide();
                    return;
                }
                
                this.Show();
                this.Activate();
                this.Topmost = true;
                this.Topmost = false;
                this.Focus();
            };
        }
    }
}
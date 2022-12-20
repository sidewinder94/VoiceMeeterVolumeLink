using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.HighPerformance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VoiceMeeter.NET;
using VoiceMeeter.NET.Extensions;
using VoiceMeeterVolumeLink.Configuration;
using VoiceMeeterVolumeLink.Consts;
using VoiceMeeterVolumeLink.ViewModels;

namespace VoiceMeeterVolumeLink
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private const string UniqueEventName = "{9001b1cc-3486-4688-b568-fc9d835db6ff}";

        // If the app is already launched, we don't use the field because we exit early
        // If the app was not already launched the field will then be populated 
        private readonly EventWaitHandle _eventWaitHandle = null!;

        public IServiceProvider ServiceProvider { get; }

        public App()
        {
            var serviceCollection = new ServiceCollection();
            this.ConfigureServices(serviceCollection);
            this.ServiceProvider = serviceCollection.BuildServiceProvider();

            // Try opening an existing EventWaitHandle
            if (EventWaitHandle.TryOpenExisting(UniqueEventName, out var existingHandle))
            {
                // If we manage, an instance of this app is already running, we notify
                existingHandle.Set();

                // And we exit
                this.Shutdown(ExitCodes.AlreadyRunning);
                return;
            }

            // If we can't, it does not exists, this is the first instance of the application, thus we create the handle
            this._eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, UniqueEventName);


            this.SingleInstanceWatcher();
        }

        private void SingleInstanceWatcher()
        {
            new Task(() =>
                {
                    while (this._eventWaitHandle.WaitOne())
                    {
                        Current.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            // could be set or removed anytime
                            if (Current.MainWindow.Equals(null)) return;

                            var mainWindow = Current.MainWindow;

                            if (mainWindow.WindowState == WindowState.Minimized ||
                                mainWindow.Visibility != Visibility.Visible)
                            {
                                mainWindow.Show();
                                mainWindow.WindowState = WindowState.Normal;
                            }

                            // According to some sources these steps are required to be sure it went to foreground.
                            mainWindow.Activate();
                            mainWindow.Topmost = true;
                            mainWindow.Topmost = false;
                            mainWindow.Focus();
                        }));
                    }
                }, TaskCreationOptions.LongRunning)
                .Start();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddEventLog();
            });
            services.AddSingleton<IConfigurationManager, ConfigurationManager>();
            services.AddVoiceMeeterClient();
            services.AddSingleton<MainWindowViewModel>();
        }

        /// <inheritdoc />
        protected override void OnExit(ExitEventArgs e)
        {
            if (e.ApplicationExitCode != ExitCodes.AlreadyRunning)
            {
                var client = this.ServiceProvider.GetRequiredService<IVoiceMeeterClient>();

                client.Logout();
            }

            base.OnExit(e);
        }
    }
}
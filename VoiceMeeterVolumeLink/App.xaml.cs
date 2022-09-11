using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using VoiceMeeter.NET;
using VoiceMeeter.NET.Extensions;
using VoiceMeeterVolumeLink.Configuration;
using VoiceMeeterVolumeLink.ViewModels;

namespace VoiceMeeterVolumeLink
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        const string AppSettingsFileName = "appsettings.json";

        public IServiceProvider ServiceProvider { get; }

        public App()
        {
            var serviceCollection = new ServiceCollection();
            this.ConfigureServices(serviceCollection);
            this.ServiceProvider = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();
            services.AddSingleton<IConfigurationManager, ConfigurationManager>();
            services.AddVoiceMeeterClient();
            services.AddSingleton<MainWindowViewModel>();
        }

        /// <inheritdoc />
        protected override void OnExit(ExitEventArgs e)
        {
            var client = this.ServiceProvider.GetRequiredService<IVoiceMeeterClient>();
                
            client.Logout();
            
            base.OnExit(e);
        }
    }
}
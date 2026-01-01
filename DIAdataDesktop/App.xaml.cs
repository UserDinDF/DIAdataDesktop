using DIAdataDesktop.ViewModels;
using DIAdataDesktop.Views.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Windows;

namespace DIAdataDesktop
{
    public partial class App : System.Windows.Application
    {
        public static IServiceProvider Services { get; private set; } = default!;
        private IHost? _host;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((ctx, services) =>
                {
                    services.AddSingleton<MainViewModel>();

                    // Controls
                    services.AddSingleton<QuotedAssetsControl>();
                    services.AddSingleton<ExchangesControl>();
                    services.AddSingleton<StartPageControl>();
                    services.AddSingleton<RwaControl>();

                    // Window
                    services.AddTransient<MainWindow>();
                })
                .Build();

            _host.Start();
            Services = _host.Services;

            var main = Services.GetRequiredService<MainWindow>();
            main.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host != null)
                await _host.StopAsync();

            _host?.Dispose();
            base.OnExit(e);
        }
    }
}

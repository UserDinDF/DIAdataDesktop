using DIAdataDesktop.ViewModels;
using DIAdataDesktop.Views.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Windows;

namespace DIAdataDesktop
{
    public partial class App : System.Windows.Application
    {
        private const string MutexName = @"Local\DIAdataDesktop_SingleInstance";
        private static Mutex? _mutex;

        public static IServiceProvider Services { get; private set; } = default!;
        private IHost? _host;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 1) Single instance gate
            bool createdNew;
            _mutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out createdNew);

            if (!createdNew)
            {
                // already running
                Shutdown();
                return;
            }

            base.OnStartup(e);

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((ctx, services) =>
                {
                    services.AddSingleton<MainViewModel>();

                    services.AddSingleton<QuotedAssetsControl>();
                    services.AddSingleton<ExchangesControl>();
                    services.AddSingleton<StartPageControl>();
                    services.AddSingleton<RwaControl>();

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
            try
            {
                if (_host != null)
                    await _host.StopAsync();
                _host?.Dispose();
            }
            finally
            {
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
                _mutex = null;
                base.OnExit(e);
            }
        }
    }
}

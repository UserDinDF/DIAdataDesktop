using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DIAdataDesktop.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DIAdataDesktop.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DiaApiClient _api = new();
        private readonly DispatcherTimer _timer;

        public QuotationViewModel Quotation { get; }
        public QuotedAssetsViewModel QuotedAssets { get; }

        public ObservableCollection<string> Watchlist { get; } = new()
        {
            "BTC","ETH","DIA","LINK","SOL","BNB","ARB","OP"
        };

        public ICollectionView WatchlistView { get; }

        [ObservableProperty] private string watchlistSearch = "";

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? error;

        [ObservableProperty] private DateTimeOffset? lastUpdate;

        public string StatusChipText => IsBusy ? "Busy: true" : "Ready";
        public string LastUpdateChipText => LastUpdate.HasValue
            ? $"Last: {LastUpdate:yyyy-MM-dd HH:mm:ss}"
            : "Last: -";

        public ObservableCollection<string> AutoRefreshIntervals { get; } = new()
        {
            "Off", "10s", "30s", "60s", "120s"
        };

        [ObservableProperty] private string selectedAutoRefreshInterval = "30s";
        [ObservableProperty] private bool isAutoRefreshEnabled = true;

        public string AutoRefreshStatusText
            => IsAutoRefreshEnabled && SelectedAutoRefreshInterval != "Off"
                ? $"Running every {SelectedAutoRefreshInterval}"
                : "Disabled";

        public MainViewModel()
        {
            Quotation = new QuotationViewModel(_api, SetBusyFromChild, SetErrorFromChild);
            QuotedAssets = new QuotedAssetsViewModel(_api, SetBusyFromChild, SetErrorFromChild);

            WatchlistView = CollectionViewSource.GetDefaultView(Watchlist);
            WatchlistView.Filter = o =>
            {
                if (o is not string s) return false;
                if (string.IsNullOrWhiteSpace(WatchlistSearch)) return true;
                return s.IndexOf(WatchlistSearch.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
            };

            _timer = new DispatcherTimer(DispatcherPriority.Background);
            _timer.Tick += async (_, __) =>
            {
                if (IsBusy) return;
                await RefreshAllAsync(isAuto: true);
            };

            ApplyTimerSettings();

            _ = LoadMetaAsync();
            _ = RefreshAllAsync();
        }

        partial void OnWatchlistSearchChanged(string value) => WatchlistView.Refresh();

        partial void OnIsBusyChanged(bool value)
        {
            OnPropertyChanged(nameof(StatusChipText));
            RefreshAllCommand.NotifyCanExecuteChanged();
            LoadMetaCommand.NotifyCanExecuteChanged();
        }

        partial void OnLastUpdateChanged(DateTimeOffset? value)
        {
            OnPropertyChanged(nameof(LastUpdateChipText));
        }

        partial void OnSelectedAutoRefreshIntervalChanged(string value)
        {
            OnPropertyChanged(nameof(AutoRefreshStatusText));
            ApplyTimerSettings();
        }

        partial void OnIsAutoRefreshEnabledChanged(bool value)
        {
            OnPropertyChanged(nameof(AutoRefreshStatusText));
            ApplyTimerSettings();
        }

        private void ApplyTimerSettings()
        {
            _timer.Stop();

            if (!IsAutoRefreshEnabled) return;
            if (SelectedAutoRefreshInterval == "Off") return;

            var seconds = SelectedAutoRefreshInterval switch
            {
                "10s" => 10,
                "30s" => 30,
                "60s" => 60,
                "120s" => 120,
                _ => 30
            };

            _timer.Interval = TimeSpan.FromSeconds(seconds);
            _timer.Start();
        }

        [RelayCommand(CanExecute = nameof(CanRunCommands))]
        private async Task RefreshAllAsync(bool isAuto = false)
        {
            try
            {
                SetBusyFromShell(true);
                Error = null;

                await Quotation.LoadQuotationAsync();

                await QuotedAssets.LoadQuotedAssetsAsync(CancellationToken.None);

                LastUpdate = DateTimeOffset.Now;
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally
            {
                SetBusyFromShell(false);
            }
        }

        [RelayCommand(CanExecute = nameof(CanRunCommands))]
        private async Task LoadMetaAsync()
        {
            try
            {
                SetBusyFromShell(true);
                Error = null;

                var chains = await _api.GetBlockchainsAsync(CancellationToken.None);
                var exchanges = await _api.GetExchangesAsync(CancellationToken.None);

                QuotedAssets.SetMeta(chains, exchanges);
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally
            {
                SetBusyFromShell(false);
            }
        }

        private bool CanRunCommands() => !IsBusy;

        private void SetBusyFromChild(bool busy) => SetBusyFromShell(busy);
        private void SetErrorFromChild(string? err) => Error = err;

        private void SetBusyFromShell(bool busy)
        {
            IsBusy = busy;
            Quotation.IsBusy = busy;
            QuotedAssets.IsBusy = busy;
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DIAdataDesktop.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DIAdataDesktop.ViewModels
{
    public partial class WatchlistWidgetViewModel : ObservableObject
    {
        private readonly QuotedAssetsViewModel _tokens;
        private readonly RwaViewModel _rwa;
        private readonly DispatcherTimer _timer;

        public ObservableCollection<WatchlistRowVM> TokenRows { get; } = new();
        public ObservableCollection<RwaWatchlistRowVM> RwaRows { get; } = new();

        [ObservableProperty] private bool isTopmost;
        [ObservableProperty] private bool showYesterday = true;
        [ObservableProperty] private bool showVolume = true;
        [ObservableProperty] private bool showUpdated = true;

        [ObservableProperty] private string statusLine = "Ready";

        [ObservableProperty] private bool showOnlyFavorites = true;

        public WatchlistWidgetViewModel(QuotedAssetsViewModel tokenSource, RwaViewModel rwaSource)
        {
            _tokens = tokenSource;
            _rwa = rwaSource;

            Refresh();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _timer.Tick += async (_, __) =>
            {
                Refresh();
                await RefreshRwaQuotesLightAsync();
            };
            _timer.Start();
        }

        partial void OnShowOnlyFavoritesChanged(bool value) => Refresh();

        [RelayCommand]
        private void Refresh()
        {
            // -------- TOKENS ----------
            var tokenSnapshot = _tokens.GetAllRowsSnapshot();
            var tokenList = ShowOnlyFavorites ? tokenSnapshot.Where(r => r.IsFavorite) : tokenSnapshot;

            var tokenRows = tokenList
                .OrderByDescending(r => r.Volume)
                .ToList();

            TokenRows.Clear();
            foreach (var r in tokenRows)
                TokenRows.Add(new WatchlistRowVM(r));

            // -------- RWA ----------
            var rwaSnapshot = _rwa.GetAllRowsSnapshot();
            var rwaList = ShowOnlyFavorites ? rwaSnapshot.Where(r => r.IsFavorite) : rwaSnapshot;

            var rwaRows = rwaList
                .OrderByDescending(r => r.Timestamp)
                .ToList();

            RwaRows.Clear();
            foreach (var r in rwaRows)
                RwaRows.Add(new RwaWatchlistRowVM(r));

            StatusLine = ShowOnlyFavorites
                ? $"Favorites: Tokens {TokenRows.Count} • RWA {RwaRows.Count}"
                : $"All: Tokens {TokenRows.Count} • RWA {RwaRows.Count}";
        }

        private async Task RefreshRwaQuotesLightAsync()
        {
            try
            {
                if (RwaRows.Count == 0) return;

                await _rwa.RefreshAllAsync();
            }
            catch
            {
            }
        }

        [RelayCommand]
        private async Task ToggleTokenFavorite(DiaQuotedAssetRow row)
        {
            if (row == null) return;
            await _tokens.ToggleFavorite(row);
            Refresh();
        }

        [RelayCommand]
        private async Task ToggleRwaFavorite(DiaRwaRow row)
        {
            if (row == null) return;
            await _rwa.ToggleFavorite(row);
            Refresh();
        }

        [RelayCommand]
        private void OpenTokenDetails(DiaQuotedAssetRow row)
        {
            if (row == null) return;

            var network = Uri.EscapeDataString(row.Blockchain ?? "");
            var address = row.Address ?? "";
            if (string.IsNullOrWhiteSpace(network) || string.IsNullOrWhiteSpace(address))
                return;

            var url = $"https://www.diadata.org/app/price/asset/{network}/{address}/";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        [RelayCommand]
        private void OpenRwaDetails(DiaRwaRow row)
        {
            if (row == null) return;

            var url = row.AppUrl;
            if (string.IsNullOrWhiteSpace(url)) return;

            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}

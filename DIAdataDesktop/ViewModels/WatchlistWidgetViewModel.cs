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
        private readonly QuotedAssetsViewModel _source;
        private readonly DispatcherTimer _timer;

        public ObservableCollection<WatchlistRowVM> Rows { get; } = new();

        [ObservableProperty] private bool isTopmost;
        [ObservableProperty] private bool showYesterday = true;
        [ObservableProperty] private bool showVolume = true;
        [ObservableProperty] private bool showUpdated = true;

        [ObservableProperty] private string statusLine = "Ready";

        // ✅ NEW: filter switch
        [ObservableProperty] private bool showOnlyFavorites = true;

        public WatchlistWidgetViewModel(QuotedAssetsViewModel source)
        {
            _source = source;

            Refresh();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _timer.Tick += (_, __) => Refresh();
            _timer.Start();
        }

        partial void OnShowOnlyFavoritesChanged(bool value)
        {
            Refresh();
        }

        [RelayCommand]
        private void Refresh()
        {
            var snapshot = _source.GetAllRowsSnapshot();

            var list = showOnlyFavorites
                ? snapshot.Where(r => r.IsFavorite)
                : snapshot;

            var rows = list
                .OrderByDescending(r => r.Volume) // optional
                .ToList();

            Rows.Clear();
            foreach (var r in rows)
                Rows.Add(new WatchlistRowVM(r));

            StatusLine = showOnlyFavorites
                ? $"Favorites: {Rows.Count}"
                : $"All: {Rows.Count}";
        }

        [RelayCommand]
        private async Task ToggleFavorite(DiaQuotedAssetRow row)
        {
            if (row == null) return;

            await _source.ToggleFavorite(row);

            // ✅ if we are in favorites-only mode, removing a favorite should hide it instantly
            Refresh();
        }

        [RelayCommand]
        private void OpenDetails(DiaQuotedAssetRow row)
        {
            if (row == null) return;

            var network = Uri.EscapeDataString(row.Blockchain ?? "");
            var address = row.Address ?? "";

            if (string.IsNullOrWhiteSpace(network) || string.IsNullOrWhiteSpace(address))
                return;

            var url = $"https://www.diadata.org/app/price/asset/{network}/{address}/";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}

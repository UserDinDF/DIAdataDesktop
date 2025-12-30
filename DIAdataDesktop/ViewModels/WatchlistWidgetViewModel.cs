using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DIAdataDesktop.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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

        public WatchlistWidgetViewModel(QuotedAssetsViewModel source)
        {
            _source = source;

            Refresh();

            // optional: auto refresh rows from current PagedRows (e.g. every 2s)
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _timer.Tick += (_, __) => Refresh();
            _timer.Start();
        }

        [RelayCommand]
        private void Refresh()
        {
            // nutzt einfach deine aktuelle Page / Filterung
            var snapshot = _source.PagedRows.ToList();

            Rows.Clear();
            foreach (var r in snapshot)
                Rows.Add(new WatchlistRowVM(r));

            StatusLine = $"Rows: {Rows.Count} • Page {_source.CurrentPage}/{_source.TotalPages}";
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

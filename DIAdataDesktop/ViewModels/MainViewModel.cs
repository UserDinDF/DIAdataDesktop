using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DIAdataDesktop.Models;
using DIAdataDesktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DIAdataDesktop.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DiaApiClient _api = new();
        private readonly DispatcherTimer _timer;
        private CancellationTokenSource? _cts;

        public ObservableCollection<string> Watchlist { get; } = new()
        {
            "DIA", "BTC", "ETH"
        };

        // NEW: simple mode switch
        // values: "Symbol" or "Address"
        [ObservableProperty] private string mode = "Symbol";

        [ObservableProperty] private string symbol = "DIA";

        // NEW: for assetQuotation
        [ObservableProperty] private string blockchain = "Bitcoin";
        [ObservableProperty] private string assetAddress = "0x0000000000000000000000000000000000000000";

        [ObservableProperty] private DiaQuotation? quote;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? error;

        public MainViewModel()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(15)
            };
            _timer.Tick += async (_, _) => await RefreshAsync();
            _timer.Start();
        }

        [RelayCommand]
        private async Task LoadAsync()
        {
            await RefreshAsync();
        }

        [RelayCommand]
        private async Task PickAsync(string pickedSymbol)
        {
            Mode = "Symbol";
            Symbol = pickedSymbol;
            await RefreshAsync();
        }

        [RelayCommand]
        private void AddToWatchlist()
        {
            var s = (Symbol ?? "").Trim().ToUpperInvariant();
            if (s.Length == 0) return;
            if (!Watchlist.Contains(s))
                Watchlist.Add(s);
        }

        [RelayCommand]
        private async Task UseAddressExampleAsync()
        {
            // Quick test button: switches to address mode with the BTC example you posted
            Mode = "Address";
            Blockchain = "Bitcoin";
            AssetAddress = "0x0000000000000000000000000000000000000000";
            await RefreshAsync();
        }

        private async Task RefreshAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            try
            {
                IsBusy = true;
                Error = null;

                if (string.Equals(Mode, "Address", StringComparison.OrdinalIgnoreCase))
                {
                    Quote = await _api.GetQuotationByAddressAsync(Blockchain, AssetAddress, _cts.Token);
                }
                else
                {
                    Quote = await _api.GetQuotationBySymbolAsync(Symbol, _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}

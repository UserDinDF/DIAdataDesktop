using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DIAdataDesktop.Models;
using DIAdataDesktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DIAdataDesktop.ViewModels
{
    public partial class QuotationViewModel : ObservableObject
    {
        private readonly DiaApiClient _api;
        private readonly Action<bool> _setBusy;
        private readonly Action<string?> _setError;

        private readonly DispatcherTimer _timer;

        public ObservableCollection<string> Watchlist { get; } = new() { "DIA", "BTC", "ETH" };

        [ObservableProperty] private string mode = "Symbol";
        [ObservableProperty] private string symbol = "DIA";
        [ObservableProperty] private string blockchain = "Bitcoin";
        [ObservableProperty] private string assetAddress = "0x0000000000000000000000000000000000000000";

        [ObservableProperty] private DiaQuotation? quote;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? error;

        private DateTimeOffset? _lastUpdated;
        public string LastUpdatedText => _lastUpdated.HasValue
            ? $"Updated: {_lastUpdated:yyyy-MM-dd HH:mm:ss}"
            : "Updated: -";

        public QuotationViewModel()
        {

        }

        public QuotationViewModel(DiaApiClient api, Action<bool> setBusy, Action<string?> setError)
        {
            _api = api;
            _setBusy = setBusy;
            _setError = setError;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
            _timer.Tick += async (_, _) => await RefreshAsync(CancellationToken.None);
            _timer.Start();
        }

        [RelayCommand]
        public async Task LoadQuotationAsync()
        {
            await RefreshAsync(CancellationToken.None);
        }

        public Task LoadQuotationAsyncPublic() => RefreshAsync(CancellationToken.None);

        public async Task RefreshAsync(CancellationToken ct)
        {
            try
            {
                _setBusy(true);
                _setError(null);

                if (string.Equals(Mode, "Address", StringComparison.OrdinalIgnoreCase))
                    Quote = await _api.GetQuotationByAddressAsync(Blockchain, AssetAddress, ct);
                else
                    Quote = await _api.GetQuotationBySymbolAsync(Symbol, ct);

                _lastUpdated = DateTimeOffset.Now;
                OnPropertyChanged(nameof(LastUpdatedText));
            }
            catch (OperationCanceledException) 
            { 

            }
            catch (Exception ex)
            {
                _setError(ex.Message);
            }
            finally
            {
                _setBusy(false);
            }
        }

        [RelayCommand]
        private async Task PickAsync(string? pickedSymbol)
        {
            if (string.IsNullOrWhiteSpace(pickedSymbol)) return;

            Mode = "Symbol";
            Symbol = pickedSymbol.Trim().ToUpperInvariant();

            await RefreshAsync(CancellationToken.None);
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
            Mode = "Address";
            Blockchain = "Bitcoin";
            AssetAddress = "0x0000000000000000000000000000000000000000";
            await RefreshAsync(CancellationToken.None);
        }
    }
}

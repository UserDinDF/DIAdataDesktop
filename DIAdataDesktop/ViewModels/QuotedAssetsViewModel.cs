using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DIAdataDesktop.Models;
using DIAdataDesktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DIAdataDesktop.ViewModels
{
    public partial class QuotedAssetsViewModel : ObservableObject
    {
        private readonly DiaApiClient _api;
        private readonly Action<bool> _setBusy;
        private readonly Action<string?> _setError;

        public ObservableCollection<string> Blockchains { get; } = new();
        public ObservableCollection<DiaExchange> Exchanges { get; } = new();

        [ObservableProperty] private string selectedBlockchain = "(All)";
        public ObservableCollection<DiaQuotedAsset> QuotedAssets { get; } = new();

        [ObservableProperty] private string searchText = "";
        [ObservableProperty] private string blockchainSearch = "";
        [ObservableProperty] private string exchangeSearch = "";
        [ObservableProperty] private string statusText = "Ready";

        [ObservableProperty] private int quotedAssetsCount;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? error;

        public ICollectionView BlockchainsView { get; }
        public ICollectionView ExchangesView { get; }
        public ICollectionView QuotedAssetsView { get; }

        public QuotedAssetsViewModel(DiaApiClient api, Action<bool> setBusy, Action<string?> setError)
        {
            _api = api;
            _setBusy = setBusy;
            _setError = setError;

            Blockchains.Add("(All)");

            BlockchainsView = CollectionViewSource.GetDefaultView(Blockchains);
            ExchangesView = CollectionViewSource.GetDefaultView(Exchanges);
            QuotedAssetsView = CollectionViewSource.GetDefaultView(QuotedAssets);

            BlockchainsView.Filter = o =>
            {
                if (o is not string s) return false;
                if (string.IsNullOrWhiteSpace(BlockchainSearch)) return true;
                return s.IndexOf(BlockchainSearch.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
            };

            ExchangesView.Filter = o =>
            {
                if (o is not DiaExchange ex) return false;

                if (string.IsNullOrWhiteSpace(ExchangeSearch))
                    return true;

                var q = ExchangeSearch.Trim();
                return (ex.Name ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (ex.Type ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (ex.Blockchain ?? "").Contains(q, StringComparison.OrdinalIgnoreCase);
            };

            QuotedAssetsView.Filter = o =>
            {
                if (o is not DiaQuotedAsset qa) return false;
                if (string.IsNullOrWhiteSpace(SearchText)) return true;

                var q = SearchText.Trim();
                var sym = qa.Asset?.Symbol ?? "";
                var name = qa.Asset?.Name ?? "";
                var addr = qa.Asset?.Address ?? "";

                return sym.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || name.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || addr.Contains(q, StringComparison.OrdinalIgnoreCase);
            };
        }

        partial void OnBlockchainSearchChanged(string value) => BlockchainsView.Refresh();
        partial void OnExchangeSearchChanged(string value) => ExchangesView.Refresh();
        partial void OnSearchTextChanged(string value)
        {
            QuotedAssetsView.Refresh();
            QuotedAssetsCount = QuotedAssetsView.Cast<object>().Count();
        }

        public void SetMeta(IEnumerable<string> blockchains, IEnumerable<DiaExchange> exchanges)
        {
            Blockchains.Clear();
            Blockchains.Add("(All)");

            foreach (var b in blockchains
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                Blockchains.Add(b);
            }

            Exchanges.Clear();
            foreach (var e in exchanges
                .Where(x => !string.IsNullOrWhiteSpace(x?.Name))
                .OrderByDescending(x => x.Volume24h))
            {
                Exchanges.Add(e);
            }

            BlockchainsView.Refresh();
            ExchangesView.Refresh();

            if (!Blockchains.Contains(SelectedBlockchain))
                SelectedBlockchain = Blockchains.FirstOrDefault() ?? "(All)";
        }


        [RelayCommand]
        private async Task LoadMetaAsync()
        {
            try
            {
                StatusText = "Loading meta...";
                _setBusy(true);
                _setError(null);

                var chains = await _api.GetBlockchainsAsync(CancellationToken.None);
                var ex = await _api.GetExchangesAsync(CancellationToken.None);

                SetMeta(chains, ex);

                StatusText = $"Meta loaded: {Blockchains.Count - 1} blockchains, {Exchanges.Count} exchanges.";
            }
            catch (Exception ex2)
            {
                StatusText = "Meta load failed";
                _setError(ex2.Message);
            }
            finally
            {
                _setBusy(false);
            }
        }


        public async Task LoadQuotedAssetsAsync(CancellationToken ct)
        {
            try
            {
                StatusText = "Loading quoted assets...";
                _setBusy(true);
                _setError(null);

                QuotedAssets.Clear();

                string? bc = SelectedBlockchain;
                if (string.Equals(bc, "(All)", StringComparison.OrdinalIgnoreCase))
                    bc = null;

                var list = await _api.GetQuotedAssetsAsync(bc, ct);

                foreach (var item in list.OrderByDescending(x => x.Volume))
                    QuotedAssets.Add(item);

                QuotedAssetsView.Refresh();
                QuotedAssetsCount = QuotedAssetsView.Cast<object>().Count();
                StatusText = $"Loaded {QuotedAssetsCount} assets.";
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                StatusText = "Error";
                _setError(ex.Message);
            }
            finally
            {
                _setBusy(false);
            }
        }


        [RelayCommand]
        private async Task LoadQuotedAssetsAsync()
        {
            await LoadQuotedAssetsAsync(CancellationToken.None);
        }
    }
}

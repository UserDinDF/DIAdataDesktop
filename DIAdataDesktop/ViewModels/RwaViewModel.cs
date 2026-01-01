using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DIAdataDesktop.Data;
using DIAdataDesktop.Models;
using DIAdataDesktop.Models.Enums;
using DIAdataDesktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Application = System.Windows.Application;

namespace DIAdataDesktop.ViewModels
{
    public partial class RwaViewModel : ObservableObject
    {
        private readonly DiaApiClient _api;
        private readonly Action<bool> _setBusy;
        private readonly Action<string?> _setError;
        private readonly Dispatcher _ui;

        private readonly FavoritesRepository _favoritesRepo;
        private HashSet<string> _favoriteKeys = new(StringComparer.OrdinalIgnoreCase);

        private readonly List<DiaRwaRow> _all = new();
        private List<DiaRwaRow> _filtered = new();

        public ObservableCollection<DiaRwaRow> PagedRows { get; } = new();

        [ObservableProperty] private string searchText = "";
        [ObservableProperty] private string statusText = "Ready";

        [ObservableProperty] private int pageSize = 18;
        [ObservableProperty] private int currentPage = 1;
        [ObservableProperty] private int totalPages = 1;

        [ObservableProperty] private int totalCount;
        [ObservableProperty] private int filteredCount;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private int parallelism = 6;

        public ObservableCollection<int> PageSizes { get; } = new() { 12, 18, 24, 36 };

        private CancellationTokenSource? _refreshCts;

        public RwaViewModel(DiaApiClient api, Action<bool> setBusy, Action<string?> setError)
        {
            _api = api;
            _setBusy = setBusy;
            _setError = setError;
            _ui = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DIAdataDesktop",
                "diadata.local.db");

            _favoritesRepo = new FavoritesRepository(dbPath);

            BuildFromYourEndpoints();
        }

        private void BuildFromYourEndpoints()
        {
            // Commodities
            Add(RwaType.Commodities, "NG", "NG-USD", "Natural Gas");
            Add(RwaType.Commodities, "WTI", "WTI-USD", "WTI Crude Oil");
            Add(RwaType.Commodities, "XBR", "XBR-USD", "Brent Crude Oil");
            Add(RwaType.Commodities, "XG", "XG-USD", "Copper");
            Add(RwaType.Commodities, "XAGG", "XAGG-USD", "Silver");
            Add(RwaType.Commodities, "XAU", "XAU-USD", "Gold");

            // Forex (note: your list includes reversed pairs too)
            Add(RwaType.Forex, "CAD", "CAD-USD", "CAD / USD");
            Add(RwaType.Forex, "AUD", "AUD-USD", "AUD / USD");
            Add(RwaType.Forex, "CNY", "CNY-USD", "CNY / USD");

            Add(RwaType.Forex, "GBP", "GBP-USD", "GBP / USD");
            Add(RwaType.Forex, "CHF", "USD-CHF", "USD / CHF");
            Add(RwaType.Forex, "JPY", "JPY-USD", "JPY / USD");
            Add(RwaType.Forex, "EUR", "EUR-USD", "EUR / USD");
            Add(RwaType.Forex, "BRL", "USD-BRL", "USD / BRL");
            Add(RwaType.Forex, "NGN", "NGN-USD", "NGN / USD");

            // ETFs
            Add(RwaType.Etf, "TLT", "TLT", "iShares 20+ Year Treasury Bond ETF");
            Add(RwaType.Etf, "SHY", "SHY", "iShares 1-3 Year Treasury Bond ETF");
            Add(RwaType.Etf, "VGSH", "VGSH", "Vanguard Short-Term Treasury ETF");
            Add(RwaType.Etf, "GOVT", "GOVT", "iShares U.S. Treasury Bond ETF");
            Add(RwaType.Etf, "BETH", "BETH", "ETF");
            Add(RwaType.Etf, "ETHA", "ETHA", "ETF");
            Add(RwaType.Etf, "BITO", "BITO", "ETF");
            Add(RwaType.Etf, "GBTC", "GBTC", "ETF");
            Add(RwaType.Etf, "HODL", "HODL", "ETF");
            Add(RwaType.Etf, "ARKB", "ARKB", "ETF");
            Add(RwaType.Etf, "FBTC", "FBTC", "ETF");
            Add(RwaType.Etf, "IBIT", "IBIT", "ETF");
            Add(RwaType.Etf, "QQQ", "QQQ", "Invesco QQQ Trust");
            Add(RwaType.Etf, "VTI", "VTI", "Vanguard Total Stock Market ETF");
            Add(RwaType.Etf, "SPY", "SPY", "SPDR S&P 500 ETF Trust");
            Add(RwaType.Etf, "VOO", "VOO", "Vanguard S&P 500 ETF");

            Add(RwaType.Etf, "EEM", "EEM", "iShares MSCI Emerging Markets ETF");
            Add(RwaType.Etf, "URTH", "URTH", "iShares MSCI World ETF");
            Add(RwaType.Etf, "IVV", "IVV", "iShares Core S&P 500 ETF");

            // Equities
            Add(RwaType.Equities, "VFS", "VFS", "VinFast Auto");
            Add(RwaType.Equities, "HOOD", "HOOD", "Robinhood Markets");
            Add(RwaType.Equities, "CME", "CME", "CME Group");
            Add(RwaType.Equities, "BLK", "BLK", "BlackRock");
            Add(RwaType.Equities, "INTC", "INTC", "Intel");
            Add(RwaType.Equities, "MU", "MU", "Micron");
            Add(RwaType.Equities, "UBER", "UBER", "Uber");
            Add(RwaType.Equities, "RTX", "RTX", "RTX");
            Add(RwaType.Equities, "SCHW", "SCHW", "Charles Schwab");
            Add(RwaType.Equities, "NKE", "NKE", "Nike");
            Add(RwaType.Equities, "COP", "COP", "ConocoPhillips");
            Add(RwaType.Equities, "NOW", "NOW", "ServiceNow");
            Add(RwaType.Equities, "GS", "GS", "Goldman Sachs");
            Add(RwaType.Equities, "UNP", "UNP", "Union Pacific");
            Add(RwaType.Equities, "IBM", "IBM", "IBM");
            Add(RwaType.Equities, "NEE", "NEE", "NextEra Energy");
            Add(RwaType.Equities, "PM", "PM", "Philip Morris");
            Add(RwaType.Equities, "CMCSA", "CMCSA", "Comcast");
            Add(RwaType.Equities, "PFE", "PFE", "Pfizer");
            Add(RwaType.Equities, "MS", "MS", "Morgan Stanley");
            Add(RwaType.Equities, "AMGN", "AMGN", "Amgen");
            Add(RwaType.Equities, "VZ", "VZ", "Verizon");
            Add(RwaType.Equities, "AMAT", "AMAT", "Applied Materials");
            Add(RwaType.Equities, "AXP", "AXP", "American Express");
            Add(RwaType.Equities, "TXN", "TXN", "Texas Instruments");
            Add(RwaType.Equities, "GE", "GE", "GE");
            Add(RwaType.Equities, "CAT", "CAT", "Caterpillar");
            Add(RwaType.Equities, "INTU", "INTU", "Intuit");
            Add(RwaType.Equities, "ABT", "ABT", "Abbott");
            Add(RwaType.Equities, "DHR", "DHR", "Danaher");
            Add(RwaType.Equities, "TMUS", "TMUS", "T-Mobile");
            Add(RwaType.Equities, "DIS", "DIS", "Disney");
            Add(RwaType.Equities, "MCD", "MCD", "McDonald's");
            Add(RwaType.Equities, "CSCO", "CSCO", "Cisco");
            Add(RwaType.Equities, "QCOM", "QCOM", "Qualcomm");
            Add(RwaType.Equities, "ADBE", "ADBE", "Adobe");
            Add(RwaType.Equities, "WFC", "WFC", "Wells Fargo");
            Add(RwaType.Equities, "TMO", "TMO", "Thermo Fisher");
            Add(RwaType.Equities, "AMD", "AMD", "AMD");
            Add(RwaType.Equities, "PEP", "PEP", "PepsiCo");
            Add(RwaType.Equities, "NFLX", "NFLX", "Netflix");
            Add(RwaType.Equities, "CRM", "CRM", "Salesforce");
            Add(RwaType.Equities, "KO", "KO", "Coca-Cola");
            Add(RwaType.Equities, "ABBV", "ABBV", "AbbVie");
            Add(RwaType.Equities, "BAC", "BAC", "Bank of America");
            Add(RwaType.Equities, "CVX", "CVX", "Chevron");
            Add(RwaType.Equities, "ORCL", "ORCL", "Oracle");
            Add(RwaType.Equities, "MRK", "MRK", "Merck");
            Add(RwaType.Equities, "HD", "HD", "Home Depot");
            Add(RwaType.Equities, "GME", "GME", "GameStop");
            Add(RwaType.Equities, "PYPL", "PYPL", "PayPal");
            Add(RwaType.Equities, "MSTR", "MSTR", "MicroStrategy");
            Add(RwaType.Equities, "COIN", "COIN", "Coinbase");
            Add(RwaType.Equities, "COST", "COST", "Costco");
            Add(RwaType.Equities, "JNJ", "JNJ", "Johnson & Johnson");
            Add(RwaType.Equities, "PG", "PG", "Procter & Gamble");
            Add(RwaType.Equities, "MA", "MA", "Mastercard");
            Add(RwaType.Equities, "UNH", "UNH", "UnitedHealth");
            Add(RwaType.Equities, "WMT", "WMT", "Walmart");
            Add(RwaType.Equities, "XOM", "XOM", "Exxon Mobil");
            Add(RwaType.Equities, "V", "V", "Visa");
            Add(RwaType.Equities, "TSLA", "TSLA", "Tesla");
            Add(RwaType.Equities, "JPM", "JPM", "JPMorgan");
            Add(RwaType.Equities, "AVGO", "AVGO", "Broadcom");
            Add(RwaType.Equities, "LLY", "LLY", "Eli Lilly");
            Add(RwaType.Equities, "BRKB", "BRK.B", "Berkshire Hathaway");
            Add(RwaType.Equities, "META", "META", "Meta Platforms");
            Add(RwaType.Equities, "AMZN", "AMZN", "Amazon");
            Add(RwaType.Equities, "MSFT", "MSFT", "Microsoft");
            Add(RwaType.Equities, "GOOG", "GOOG", "Alphabet");
            Add(RwaType.Equities, "NVDA", "NVDA", "NVIDIA");
            Add(RwaType.Equities, "AAPL", "AAPL", "Apple");
        }

        private void Add(RwaType type, string appSlug, string apiTicker, string name)
            => _all.Add(new DiaRwaRow(type, appSlug, apiTicker, name));

        public async Task InitializeAsync(CancellationToken ct = default)
        {
            await LoadFavoritesAsync(ct);

            CurrentPage = 1;
            ApplyFilterCore();
            ApplyPagingCore();

            await RefreshVisiblePageAsync(ct);
        }

        partial void OnSearchTextChanged(string value)
        {
            CurrentPage = 1;
            ApplyFilterAndPagingUiSafe();
        }

        partial void OnPageSizeChanged(int value)
        {
            if (value <= 0) PageSize = 18;
            CurrentPage = 1;
            ApplyFilterAndPagingUiSafe();
        }

        partial void OnCurrentPageChanged(int value)
        {
            ApplyPagingUiSafe();
            PrevPageCommand.NotifyCanExecuteChanged();
            NextPageCommand.NotifyCanExecuteChanged();
            _ = RefreshVisiblePageAsync();
        }

        private bool CanPrev() => CurrentPage > 1 && !IsBusy;
        private bool CanNext() => CurrentPage < TotalPages && !IsBusy;

        [RelayCommand(CanExecute = nameof(CanPrev))]
        private void PrevPage() => CurrentPage--;

        [RelayCommand(CanExecute = nameof(CanNext))]
        private void NextPage() => CurrentPage++;

        [RelayCommand] private void GoToFirst() => CurrentPage = 1;
        [RelayCommand] private void GoToLast() => CurrentPage = TotalPages;

        [RelayCommand]
        public Task RefreshAllAsync() => RefreshVisiblePageAsync();

        private async Task RefreshVisiblePageAsync(CancellationToken ct = default)
        {
            _refreshCts?.Cancel();
            _refreshCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            ct = _refreshCts.Token;

            var rows = await _ui.InvokeAsync(() => PagedRows.ToList());
            if (rows.Count == 0) return;

            try
            {
                await _ui.InvokeAsync(() =>
                {
                    StatusText = "Loading RWA quotes...";
                    _setBusy(true);
                    _setError(null);
                });

                using var sem = new SemaphoreSlim(Math.Max(1, Parallelism));

                var tasks = rows.Select(async r =>
                {
                    await sem.WaitAsync(ct);
                    try
                    {
                        var q = await _api.GetRwaAsync(r.Type, r.ApiTicker, ct);

                        await _ui.InvokeAsync(() =>
                        {
                            r.ApplyQuote(q);
                        }, DispatcherPriority.Background);
                    }
                    catch (OperationCanceledException) { }
                    catch { }
                    finally { sem.Release(); }
                }).ToArray();

                await Task.WhenAll(tasks);
                await _ui.InvokeAsync(() => StatusText = "RWA ready.");
            }
            catch (OperationCanceledException)
            {
                await _ui.InvokeAsync(() => StatusText = "Canceled.");
            }
            catch (Exception ex)
            {
                await _ui.InvokeAsync(() =>
                {
                    StatusText = "RWA error";
                    _setError(ex.Message);
                });
            }
            finally
            {
                await _ui.InvokeAsync(() => _setBusy(false));
            }
        }

        public async Task ToggleFavorite(DiaRwaRow? row)
        {
            if (row == null) return;

            try
            {
                row.IsFavorite = !row.IsFavorite;

                if (row.IsFavorite)
                {
                    _favoriteKeys.Add(row.FavKey);
                    await _favoritesRepo.UpsertAsync(
                        kind: "rwa",
                        key: row.FavKey,
                        name: row.AppSlug,
                        extra1: row.Type.ToString(),
                        extra2: row.ApiUrl);
                }
                else
                {
                    _favoriteKeys.Remove(row.FavKey);
                    await _favoritesRepo.RemoveAsync("rwa", row.FavKey);
                }
            }
            catch (Exception ex)
            {
                row.IsFavorite = !row.IsFavorite;
                _setError(ex.Message);
            }
        }

        private async Task LoadFavoritesAsync(CancellationToken ct = default)
        {
            await _favoritesRepo.EnsureCreatedAsync(ct);
            _favoriteKeys = await _favoritesRepo.GetKeysAsync("rwa", ct);

            await _ui.InvokeAsync(() =>
            {
                foreach (var r in _all)
                    r.IsFavorite = _favoriteKeys.Contains(r.FavKey);
            });
        }

        private void ApplyFilterAndPagingUiSafe()
        {
            if (_ui.CheckAccess())
            {
                ApplyFilterCore();
                ApplyPagingCore();
            }
            else
            {
                _ui.BeginInvoke((Action)(() =>
                {
                    ApplyFilterCore();
                    ApplyPagingCore();
                }), DispatcherPriority.Background);
            }
        }

        private void ApplyPagingUiSafe()
        {
            if (_ui.CheckAccess())
                ApplyPagingCore();
            else
                _ui.BeginInvoke((Action)ApplyPagingCore, DispatcherPriority.Background);
        }

        private void ApplyFilterCore()
        {
            var q = (SearchText ?? "").Trim();

            IEnumerable<DiaRwaRow> filtered = _all;

            if (!string.IsNullOrWhiteSpace(q))
            {
                filtered = filtered.Where(x =>
                    x.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    x.AppSlug.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    x.ApiTicker.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    x.AppUrl.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    x.ApiUrl.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    x.TypeLabel.Contains(q, StringComparison.OrdinalIgnoreCase));
            }

            _filtered = filtered.ToList();

            TotalCount = _all.Count;
            FilteredCount = _filtered.Count;

            TotalPages = Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)PageSize));
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;
        }

        private void ApplyPagingCore()
        {
            TotalPages = Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)PageSize));
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            var skip = (CurrentPage - 1) * PageSize;
            var page = _filtered.Skip(skip).Take(PageSize).ToList();

            PagedRows.Clear();
            foreach (var r in page)
                PagedRows.Add(r);

            PrevPageCommand.NotifyCanExecuteChanged();
            NextPageCommand.NotifyCanExecuteChanged();
        }
    }
}

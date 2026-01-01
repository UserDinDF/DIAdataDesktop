using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DIAdataDesktop.Data;
using DIAdataDesktop.Models;
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
    public partial class QuotedAssetsViewModel : ObservableObject
    {
        private readonly DiaApiClient _api;
        private readonly Action<bool> _setBusy;
        private readonly Action<string?> _setError;
        private readonly Dispatcher _ui;

        private readonly FavoritesRepository _favoritesRepo;
        private HashSet<string> _favoriteKeys = new(StringComparer.OrdinalIgnoreCase);

        private CancellationTokenSource? _loadCts;
        private CancellationTokenSource? _reloadDebounceCts;

        // Prefetch debounce + cancellation
        private CancellationTokenSource? _prefetchCts;
        private CancellationTokenSource? _prefetchDebounceCts;

        // Caches (in-memory)
        private readonly Dictionary<string, CacheEntry<DiaQuotation>> _quotationCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, CacheEntry<List<DiaCexPairsByAssetRow>>> _pairsCache = new(StringComparer.OrdinalIgnoreCase);

        // TTLs (tune)
        private readonly TimeSpan _quotationTtl = TimeSpan.FromSeconds(20);
        private readonly TimeSpan _pairsTtl = TimeSpan.FromMinutes(10);

        private readonly object _cacheLock = new();

        // ComboBox source
        public ObservableCollection<string> Blockchains { get; } = new() { "(All)" };

        // Alles / Gefiltert (UI-thread only)
        private readonly List<DiaQuotedAssetRow> _allRows = new();
        private List<DiaQuotedAssetRow> _filteredRows = new();

        // Seite (UI bindet hier)
        public ObservableCollection<DiaQuotedAssetRow> PagedRows { get; } = new();

        [ObservableProperty] private string selectedBlockchain = "(All)";
        [ObservableProperty] private string searchText = "";
        [ObservableProperty] private string statusText = "Ready";

        // Paging
        [ObservableProperty] private int pageSize = 30;
        [ObservableProperty] private int currentPage = 1;
        [ObservableProperty] private int totalPages = 1;

        // Counts
        [ObservableProperty] private int totalCount;
        [ObservableProperty] private int filteredCount;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? error;

        // Prefetch tuning
        [ObservableProperty] private int quotationParallelism = 8;

        public QuotedAssetsViewModel(DiaApiClient api, Action<bool> setBusy, Action<string?> setError)
        {
            _api = api;
            _setBusy = setBusy;
            _setError = setError;
            _ui = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DIAdataDesktop", "diadata.local.db");

            _favoritesRepo = new FavoritesRepository(dbPath);
        }

        private static string KeyOf(DiaQuotedAsset a) => $"{(a.Asset.Blockchain ?? "").Trim()}|{(a.Asset.Address ?? "").Trim()}".ToLowerInvariant();

        private static string KeyOf(DiaQuotedAssetRow r) => $"{(r.Blockchain ?? "").Trim()}|{(r.Address ?? "").Trim()}".ToLowerInvariant();

        private static string QuoteKey(string symbol) => (symbol ?? "").Trim().ToUpperInvariant();
        private static string PairsKey(string blockchain, string address) => $"{(blockchain ?? "").Trim()}|{(address ?? "").Trim()}".ToLowerInvariant();

        private sealed class CacheEntry<T>
        {
            public CacheEntry(T value, DateTimeOffset fetchedAt)
            {
                Value = value;
                FetchedAt = fetchedAt;
            }
            public T Value { get; }
            public DateTimeOffset FetchedAt { get; }
        }

        private bool TryGetQuotationCached(string symbol, out DiaQuotation quote)
        {
            quote = default!;
            var key = QuoteKey(symbol);
            var now = DateTimeOffset.UtcNow;

            lock (_cacheLock)
            {
                if (_quotationCache.TryGetValue(key, out var e) && (now - e.FetchedAt) <= _quotationTtl)
                {
                    quote = e.Value;
                    return true;
                }
            }
            return false;
        }

        private void SetQuotationCache(string symbol, DiaQuotation quote)
        {
            var key = QuoteKey(symbol);
            lock (_cacheLock)
                _quotationCache[key] = new CacheEntry<DiaQuotation>(quote, DateTimeOffset.UtcNow);
        }

        private bool TryGetPairsCached(string blockchain, string address, out List<DiaCexPairsByAssetRow> pairs)
        {
            pairs = default!;
            var key = PairsKey(blockchain, address);
            var now = DateTimeOffset.UtcNow;

            lock (_cacheLock)
            {
                if (_pairsCache.TryGetValue(key, out var e) && (now - e.FetchedAt) <= _pairsTtl)
                {
                    pairs = e.Value;
                    return true;
                }
            }
            return false;
        }

        private void SetPairsCache(string blockchain, string address, List<DiaCexPairsByAssetRow> pairs)
        {
            var key = PairsKey(blockchain, address);
            lock (_cacheLock)
                _pairsCache[key] = new CacheEntry<List<DiaCexPairsByAssetRow>>(pairs, DateTimeOffset.UtcNow);
        }

        public async Task InitializeAsync(CancellationToken ct = default)
        {
            try
            {
                await _ui.InvokeAsync(() =>
                {
                    StatusText = "Loading blockchains...";
                    _setError(null);
                });

                var blockhains = await _api.GetBlockchainsAsync(ct);

                await _ui.InvokeAsync(() =>
                {
                    Blockchains.Clear();
                    Blockchains.Add("(All)");

                    foreach (var b in blockhains
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
                    {
                        Blockchains.Add(b);
                    }

                    if (!Blockchains.Contains(SelectedBlockchain))
                        SelectedBlockchain = "(All)";

                    StatusText = "Ready";
                });
            }
            catch (Exception ex)
            {
                await _ui.InvokeAsync(() =>
                {
                    StatusText = "Meta load failed";
                    _setError(ex.Message);
                });
            }
        }
        public async Task LoadFavoritesAsync(CancellationToken ct = default)
        {
            await _favoritesRepo.EnsureCreatedAsync(ct);
            _favoriteKeys = await _favoritesRepo.GetKeysAsync("token", ct);

            await _ui.InvokeAsync(() =>
            {
                foreach (var r in _allRows)
                {
                    var key = FavoritesRepository.MakeTokenKey(r.Blockchain, r.Address);
                    r.IsFavorite = _favoriteKeys.Contains(key);
                }
            });
        }


        public IReadOnlyList<DiaQuotedAssetRow> GetAllRowsSnapshot()
        {
            if (_ui.CheckAccess())
                return _allRows.ToList();

            return _ui.Invoke(() => _allRows.ToList());
        }


        partial void OnSearchTextChanged(string value)
        {
            CurrentPage = 1;
            ApplyFilterAndPagingUiSafe();
            DebouncedPrefetchVisible();
        }

        partial void OnSelectedBlockchainChanged(string value)
        {
            DebouncedReload();
        }

        private void DebouncedReload(int ms = 250)
        {
            _reloadDebounceCts?.Cancel();
            _reloadDebounceCts = new CancellationTokenSource();
            var token = _reloadDebounceCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(ms, token);
                    await LoadQuotedAssetsAsync();
                }
                catch (OperationCanceledException) { }
            });
        }

        partial void OnPageSizeChanged(int value)
        {
            if (value <= 0) PageSize = 30;
            CurrentPage = 1;
            ApplyFilterAndPagingUiSafe();
            DebouncedPrefetchVisible();
        }

        partial void OnCurrentPageChanged(int value)
        {
            ApplyPagingUiSafe();
            NextPageCommand.NotifyCanExecuteChanged();
            PrevPageCommand.NotifyCanExecuteChanged();

            DebouncedPrefetchVisible();
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
            var bc = SelectedBlockchain;

            IEnumerable<DiaQuotedAssetRow> filtered = _allRows;

            if (!string.IsNullOrWhiteSpace(bc) && !string.Equals(bc, "(All)", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(r =>
                    (r.Blockchain ?? "").Equals(bc, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                filtered = filtered.Where(r =>
                    (r.Symbol ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (r.Name ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (r.Address ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (r.Blockchain ?? "").Contains(q, StringComparison.OrdinalIgnoreCase));
            }

            _filteredRows = filtered.ToList();

            TotalCount = _allRows.Count;
            FilteredCount = _filteredRows.Count;

            TotalPages = Math.Max(1, (int)Math.Ceiling(_filteredRows.Count / (double)PageSize));
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;
        }

        private void ApplyPagingCore()
        {
            TotalPages = Math.Max(1, (int)Math.Ceiling(_filteredRows.Count / (double)PageSize));
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            var skip = (CurrentPage - 1) * PageSize;
            var page = _filteredRows.Skip(skip).Take(PageSize).ToList();

            PagedRows.Clear();
            foreach (var r in page)
                PagedRows.Add(r);

            NextPageCommand.NotifyCanExecuteChanged();
            PrevPageCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanPrev))]
        private void PrevPage() => CurrentPage--;

        private bool CanPrev() => CurrentPage > 1 && !IsBusy;

        [RelayCommand(CanExecute = nameof(CanNext))]
        private void NextPage() => CurrentPage++;

        private bool CanNext() => CurrentPage < TotalPages && !IsBusy;

        [RelayCommand] private void GoToFirst() => CurrentPage = 1;
        [RelayCommand] private void GoToLast() => CurrentPage = TotalPages;

        [RelayCommand]
        public async Task LoadQuotedAssetsAsync()
        {
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();
            var ct = _loadCts.Token;

            try
            {
                await _ui.InvokeAsync(() =>
                {
                    StatusText = "Loading assets list...";
                    _setBusy(true);
                    _setError(null);
                });

                string? bc = await _ui.InvokeAsync(() => SelectedBlockchain);
                if (string.Equals(bc, "(All)", StringComparison.OrdinalIgnoreCase))
                    bc = null;

                var list = await _api.GetQuotedAssetsAsync(bc, ct);
                var ordered = list.OrderByDescending(x => x.Volume).ToList();

                await _ui.InvokeAsync(() =>
                {
                    MergeAllRows(ordered);

                    CurrentPage = 1;
                    ApplyFilterCore();
                    ApplyPagingCore();

                    StatusText = $"Loaded {TotalCount} assets. Showing page {CurrentPage}/{TotalPages}.";
                });

                DebouncedPrefetchVisible();
            }
            catch (OperationCanceledException)
            {
                await _ui.InvokeAsync(() => StatusText = "Canceled.");
            }
            catch (Exception ex)
            {
                await _ui.InvokeAsync(() =>
                {
                    StatusText = "Error";
                    _setError(ex.Message);
                });
            }
            finally
            {
                await _ui.InvokeAsync(() =>
                {
                    _setBusy(false);
                    PrevPageCommand.NotifyCanExecuteChanged();
                    NextPageCommand.NotifyCanExecuteChanged();
                });
            }

            await LoadFavoritesAsync();
        }

        public async Task ToggleFavorite(DiaQuotedAssetRow? row)
        {
            if (row == null) return;

            try
            {
                row.IsFavorite = !row.IsFavorite;

                var key = FavoritesRepository.MakeTokenKey(row.Blockchain, row.Address);

                if (row.IsFavorite)
                {
                    _favoriteKeys.Add(key);

                    await _favoritesRepo.UpsertAsync(
                        kind: "token",
                        key: key,
                        name: row.Symbol,
                        extra1: row.Blockchain,   // optional
                        extra2: row.Address       // optional
                    );
                }
                else
                {
                    _favoriteKeys.Remove(key);
                    await _favoritesRepo.RemoveAsync("token", key);
                }
            }
            catch (Exception ex)
            {
                row.IsFavorite = !row.IsFavorite;
                _setError(ex.Message);
            }
        }

        private void MergeAllRows(List<DiaQuotedAsset> ordered)
        {
            var map = new Dictionary<string, DiaQuotedAssetRow>();

            foreach (var r in _allRows)
            {
                var key = KeyOf(r);
                if (!map.ContainsKey(key))
                    map[key] = r;
            }

            foreach (var a in ordered)
            {
                var key = KeyOf(a);
                if (map.TryGetValue(key, out var existing))
                {
                    existing.UpdateFrom(a);
                }
                else
                {
                    var row = new DiaQuotedAssetRow(a);
                    _allRows.Add(row);
                    map[key] = row;
                }
            }

            var alive = new HashSet<string>(ordered.Select(KeyOf));
            _allRows.RemoveAll(r => !alive.Contains(KeyOf(r)));

            _allRows.Sort((x, y) => y.Volume.CompareTo(x.Volume));
        }

        private void DebouncedPrefetchVisible(int ms = 200)
        {
            _prefetchDebounceCts?.Cancel();
            _prefetchDebounceCts = new CancellationTokenSource();
            var token = _prefetchDebounceCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(ms, token);
                    await PrefetchVisiblePageAsync();
                }
                catch (OperationCanceledException) { }
            });
        }

        private async Task PrefetchVisiblePageAsync()
        {
            _prefetchCts?.Cancel();
            _prefetchCts = new CancellationTokenSource();
            var ct = _prefetchCts.Token;

            var rows = await _ui.InvokeAsync(() => PagedRows.ToList());
            if (rows.Count == 0) return;

            using var sem = new SemaphoreSlim(Math.Max(1, QuotationParallelism));

            var tasks = rows.Select(async row =>
            {
                await sem.WaitAsync(ct);
                try
                {
                    var sym = row.Symbol; 
                    if (string.IsNullOrWhiteSpace(sym)) return;

                    DiaQuotation quote;
                    if (!TryGetQuotationCached(sym, out quote))
                    {
                        quote = await _api.GetQuotationBySymbolAsync(sym, ct);
                        SetQuotationCache(sym, quote);
                    }

                    List<DiaCexPairsByAssetRow>? pairs = null;
                    if (!TryGetPairsCached(quote.Blockchain, quote.Address, out var cachedPairs))
                    {
                        if (row.CexPairs == null || row.CexPairs.Count == 0)
                        {
                            pairs = await _api.GetPairsAssetCexAsync(quote.Blockchain, quote.Address, ct: ct);
                            SetPairsCache(quote.Blockchain, quote.Address, pairs);
                        }
                    }
                    else
                    {
                        if (row.CexPairs == null || row.CexPairs.Count == 0)
                            pairs = cachedPairs;
                    }

                    await _ui.InvokeAsync(() =>
                    {
                        row.UpdateQuotation(quote);

                        if (pairs != null)
                        {
                            row.CexPairs.Clear();
                            foreach (var p in pairs)
                                row.CexPairs.Add(p);

                            row.RecalcCexCounts();
                        }
                    }, DispatcherPriority.Background);
                }
                finally
                {
                    sem.Release();
                }
            }).ToArray();

            await Task.WhenAll(tasks);
        }
    }
}

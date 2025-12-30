using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DIAdataDesktop.Models;
using DIAdataDesktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace DIAdataDesktop.ViewModels
{
    public partial class QuotedAssetsViewModel : ObservableObject
    {
        private readonly DiaApiClient _api;
        private readonly Action<bool> _setBusy;
        private readonly Action<string?> _setError;
        private readonly Dispatcher _ui;

        private CancellationTokenSource? _loadCts;
        private CancellationTokenSource? _reloadDebounceCts;

        // ✅ ComboBox source
        public ObservableCollection<string> Blockchains { get; } = new() { "(All)" };


        // ✅ Alles / Gefiltert (UI-thread only)
        private readonly List<DiaQuotedAssetRow> _allRows = new();
        private List<DiaQuotedAssetRow> _filteredRows = new();

        // ✅ Seite (UI bindet hier)
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

        // Quotation prefetch
        [ObservableProperty] private int quotationPrefetchCount = 150;
        [ObservableProperty] private int quotationParallelism = 8;

        public QuotedAssetsViewModel(DiaApiClient api, Action<bool> setBusy, Action<string?> setError)
        {
            _api = api;
            _setBusy = setBusy;
            _setError = setError;
            _ui = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        }

        // ✅ public init: lädt Blockchains aus API
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

        partial void OnSearchTextChanged(string value)
        {
            CurrentPage = 1;
            ApplyFilterAndPagingUiSafe();
        }

        partial void OnSelectedBlockchainChanged(string value)
        {
            // ✅ Wenn Blockchain wirklich gewechselt wurde: server reload (debounced)
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
        }

        partial void OnCurrentPageChanged(int value)
        {
            ApplyPagingUiSafe();
            NextPageCommand.NotifyCanExecuteChanged();
            PrevPageCommand.NotifyCanExecuteChanged();
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

        // UI thread only
        private void ApplyFilterCore()
        {
            var q = (SearchText ?? "").Trim();
            var bc = SelectedBlockchain;

            IEnumerable<DiaQuotedAssetRow> filtered = _allRows;

            // ✅ lokale Filterung auf bereits geladene Rows (optional zusätzlich zu server-filter)
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

        // UI thread only
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

        [RelayCommand]
        private void GoToFirst() => CurrentPage = 1;

        [RelayCommand]
        private void GoToLast() => CurrentPage = TotalPages;

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

                // ✅ SelectedBlockchain UI-sicher lesen
                string? bc = await _ui.InvokeAsync(() => SelectedBlockchain);
                if (string.Equals(bc, "(All)", StringComparison.OrdinalIgnoreCase))
                    bc = null;

                // ✅ API call (server-side filter)
                var list = await _api.GetQuotedAssetsAsync(bc, ct);
           

                var ordered = list.OrderByDescending(x => x.Volume).ToList();

                await _ui.InvokeAsync(() =>
                {
                    _allRows.Clear();
                    foreach (var item in ordered)
                        _allRows.Add(new DiaQuotedAssetRow(item));

                    CurrentPage = 1;
                    ApplyFilterCore();
                    ApplyPagingCore();

                    StatusText = $"Loaded {TotalCount} assets. Showing page {CurrentPage}/{TotalPages}. Prefetching quotations...";
                });

                // Prefetch top N
                await PrefetchTopQuotationsAsync(topN: QuotationPrefetchCount, ct: ct);

                await _ui.InvokeAsync(() =>
                {
                    StatusText = $"Ready. Page {CurrentPage}/{TotalPages}. (Quotations prefetched: {QuotationPrefetchCount})";
                });
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
        }

        private async Task PrefetchTopQuotationsAsync(int topN, CancellationToken ct)
        {
            if (topN <= 0) return;

            var rows = await _ui.InvokeAsync(() => _allRows.Take(Math.Min(topN, _allRows.Count)).ToList());
            if (rows.Count == 0) return;

            using var sem = new SemaphoreSlim(Math.Max(1, QuotationParallelism));

            var tasks = rows.Select(async row =>
            {
                await sem.WaitAsync(ct);
                try
                {
                    var sym = row.Asset?.Symbol;
                    if (string.IsNullOrWhiteSpace(sym)) return;

                    var q = await _api.GetQuotationBySymbolAsync(sym, ct);
                    var ex =  await _api.GetPairsAssetCexAsync(q.Blockchain, q.Address);
                   
                    await _ui.InvokeAsync(() =>
                    {
                        row.Quotation = q;
                        row.CexPairs = new ObservableCollection<DiaCexPairsByAssetRow>(ex);
                        row.RecalcCexCounts();
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

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
    public partial class ExchangesViewModel : ObservableObject
    {
        private readonly DiaApiClient _api;
        private readonly Action<bool> _setBusy;
        private readonly Action<string?> _setError;
        private readonly Dispatcher _ui;

        public readonly List<DiaExchange> _all = new();
        private List<DiaExchange> _filtered = new();

        private readonly FavoritesRepository _favoritesRepo;
        private HashSet<string> _favoriteKeys = new(StringComparer.OrdinalIgnoreCase);

        public ObservableCollection<DiaExchange> PagedRows { get; } = new();

        [ObservableProperty] private string searchText = "";
        [ObservableProperty] private string statusText = "Ready";

        [ObservableProperty] private int pageSize = 30;
        [ObservableProperty] private int currentPage = 1;
        [ObservableProperty] private int totalPages = 1;

        [ObservableProperty] private int totalCount;
        [ObservableProperty] private int filteredCount;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? error;

        public ObservableCollection<int> PageSizes { get; } = new() { 15, 30, 60, 120 };

        private bool _loadedOnce;

        public ExchangesViewModel(DiaApiClient api, Action<bool> setBusy, Action<string?> setError)
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
        }

        private static string ExchangeKey(string? name) => FavoritesRepository.MakeExchangeKey(name);

        partial void OnSearchTextChanged(string value)
        {
            CurrentPage = 1;
            ApplyFilterAndPagingUiSafe();
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
            PrevPageCommand.NotifyCanExecuteChanged();
            NextPageCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanPrev))]
        private void PrevPage() => CurrentPage--;

        private bool CanPrev() => CurrentPage > 1 && !IsBusy;

        [RelayCommand(CanExecute = nameof(CanNext))]
        private void NextPage() => CurrentPage++;

        private bool CanNext() => CurrentPage < TotalPages && !IsBusy;

        [RelayCommand] private void GoToFirst() => CurrentPage = 1;
        [RelayCommand] private void GoToLast() => CurrentPage = TotalPages;

        public async Task InitializeAsync(CancellationToken ct = default)
        {
            if (_loadedOnce)
            {
                await _ui.InvokeAsync(() => StatusText = $"Ready. Loaded {TotalCount} exchanges.");
                return;
            }

            await FetchAndApplyAsync(merge: false, ct);
            _loadedOnce = true;
        }

        public async Task RefreshSnapshotAsync(CancellationToken ct = default)
            => await FetchAndApplyAsync(merge: true, ct);

        [RelayCommand]
        public async Task RefreshAsync()
        {
            _loadedOnce = false;
            await InitializeAsync();
        }

        public async Task ToggleFavorite(DiaExchange? ex)
        {
            if (ex == null) return;

            var key = ExchangeKey(ex.Name);

            try
            {
                ex.IsFavorite = !ex.IsFavorite;

                if (ex.IsFavorite)
                {
                    _favoriteKeys.Add(key);
                    await _favoritesRepo.UpsertAsync(
                        kind: "exchange",
                        key: key,
                        name: ex.Name,
                        extra1: ex.Type,
                        extra2: ex.Blockchain);
                }
                else
                {
                    _favoriteKeys.Remove(key);
                    await _favoritesRepo.RemoveAsync("exchange", key);
                }
            }
            catch (Exception err)
            {
                ex.IsFavorite = !ex.IsFavorite; // rollback
                _setError(err.Message);
            }
        }

        public async Task ToggleFavoriteByName(string? name)
        {
            var ex = _all.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            if (ex != null) await ToggleFavorite(ex);
        }

        private async Task FetchAndApplyAsync(bool merge, CancellationToken ct)
        {
            try
            {
                await _ui.InvokeAsync(() =>
                {
                    StatusText = merge ? "Refreshing exchanges..." : "Loading exchanges...";
                    _setBusy(true);
                    _setError(null);
                });

                var list = await _api.GetExchangesAsync(ct);

                var ordered = list
                    .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Name))
                    .OrderByDescending(x => x.Volume24h)
                    .ToList();

                foreach (var item in ordered)
                    item.LogoSvgPath = new Uri($"pack://application:,,,/Logos/Exchanges/{item.Name}.svg", UriKind.Absolute);

                await _ui.InvokeAsync(() =>
                {
                    if (merge) MergeAllInPlace(ordered);
                    else
                    {
                        _all.Clear();
                        _all.AddRange(ordered);
                        TotalCount = _all.Count;
                    }

                    CurrentPage = 1;
                    ApplyFilterCore();
                    ApplyPagingCore();

                    StatusText = merge
                        ? $"Updated {TotalCount} exchanges."
                        : $"Loaded {TotalCount} exchanges.";
                });

                await LoadFavoritesAsync(ct);
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

        private async Task LoadFavoritesAsync(CancellationToken ct = default)
        {
            await _favoritesRepo.EnsureCreatedAsync(ct);
            _favoriteKeys = await _favoritesRepo.GetKeysAsync("exchange", ct);

            await _ui.InvokeAsync(() =>
            {
                foreach (var ex in _all)
                    ex.IsFavorite = _favoriteKeys.Contains(ExchangeKey(ex.Name));
            });
        }

        private void MergeAllInPlace(List<DiaExchange> latest)
        {
            var map = _all.ToDictionary(
                x => (x.Name ?? "").Trim(),
                x => x,
                StringComparer.OrdinalIgnoreCase);

            foreach (var incoming in latest)
            {
                var key = (incoming.Name ?? "").Trim();
                if (string.IsNullOrWhiteSpace(key)) continue;

                if (map.TryGetValue(key, out var existing))
                {
                    existing.Type = incoming.Type;
                    existing.Blockchain = incoming.Blockchain;
                    existing.Volume24h = incoming.Volume24h;
                    existing.ScraperActive = incoming.ScraperActive;
                    existing.Name = incoming.Name;
                    existing.Pairs = incoming.Pairs;
                    existing.Trades = incoming.Trades;
                    existing.LogoSvgPath ??= new Uri($"pack://application:,,,/Logos/Exchanges/{existing.Name}.svg", UriKind.Absolute);
                }
                else
                {
                    incoming.LogoSvgPath = new Uri($"pack://application:,,,/Logos/Exchanges/{incoming.Name}.svg", UriKind.Absolute);
                    _all.Add(incoming);
                    map[key] = incoming;
                }
            }

            var alive = new HashSet<string>(latest.Select(x => (x.Name ?? "").Trim()), StringComparer.OrdinalIgnoreCase);
            _all.RemoveAll(x => !alive.Contains((x.Name ?? "").Trim()));

            _all.Sort((a, b) => b.Volume24h.CompareTo(a.Volume24h));
            TotalCount = _all.Count;
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

            IEnumerable<DiaExchange> filtered = _all;

            if (!string.IsNullOrWhiteSpace(q))
            {
                filtered = filtered.Where(x =>
                    (x.Name ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (x.Type ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (x.Blockchain ?? "").Contains(q, StringComparison.OrdinalIgnoreCase));
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

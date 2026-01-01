using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DIAdataDesktop.Models;
using DIAdataDesktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
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

        // optional: PageSizes wie bei dir
        public ObservableCollection<int> PageSizes { get; } = new() { 15, 30, 60, 120 };

        private bool _loadedOnce;

        public ExchangesViewModel(DiaApiClient api, Action<bool> setBusy, Action<string?> setError)
        {
            _api = api;
            _setBusy = setBusy;
            _setError = setError;
            _ui = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        }

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
            NextPageCommand.NotifyCanExecuteChanged();
            PrevPageCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanPrev))]
        private void PrevPage() => CurrentPage--;

        private bool CanPrev() => CurrentPage > 1 && !IsBusy;

        [RelayCommand(CanExecute = nameof(CanNext))]
        private void NextPage()
        {
            if (CurrentPage < TotalPages)
                CurrentPage++;
        }

        private bool CanNext() => CurrentPage < TotalPages && !IsBusy;

        [RelayCommand] private void GoToFirst() => CurrentPage = 1;
        [RelayCommand] private void GoToLast() => CurrentPage = TotalPages;

        public async Task InitializeAsync(CancellationToken ct = default)
        {
            if (_loadedOnce) 
            {
                await _ui.InvokeAsync(() =>
                {
                    StatusText = $"Ready. Loaded {TotalCount} exchanges.";
                });
                return;
            }

            try
            {
                await _ui.InvokeAsync(() =>
                {
                    StatusText = "Loading exchanges...";
                    _setBusy(true);
                    _setError(null);
                });

                var list = await _api.GetExchangesAsync(ct);

                // ✅ sinnvoll sortieren: zuerst nach Volume24h, dann Name
                var ordered = list
                    .Where(x => x != null && !string.IsNullOrWhiteSpace(x.Name))
                    .OrderByDescending(x => x.Volume24h)
                    .ToList();


                //set logos
                foreach (var item in ordered)
                {
                    //ex.LogoSvgUri = new Uri($"pack://application:,,,/Logos/Exchanges/{ex.Name}.svg", UriKind.Absolute);

                    item.LogoSvgPath = new Uri($"pack://application:,,,/Logos/Exchanges/{item.Name}.svg", UriKind.Absolute);
                }

                await _ui.InvokeAsync(() =>
                {
                    _all.Clear();
                    _all.AddRange(ordered);

                    _loadedOnce = true;

                    CurrentPage = 1;
                    ApplyFilterCore();
                    ApplyPagingCore();

                    StatusText = $"Loaded {TotalCount} exchanges.";
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

        [RelayCommand]
        public async Task RefreshAsync()
        {
            // wenn du wirklich mal refresh willst -> bewusst
            _loadedOnce = false;
            await InitializeAsync();
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

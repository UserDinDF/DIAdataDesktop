using DIAdataDesktop.Models;
using DIAdataDesktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using UserControl = System.Windows.Controls.UserControl;

namespace DIAdataDesktop.Views.Controls
{
    public partial class QuotedAssetDetailsControl : UserControl, INotifyPropertyChanged
    {
        private readonly DiaApiClient _api;
        private readonly DispatcherTimer _lastTradesTimer;

        private bool _isLoaded;
        private CancellationTokenSource? _lastTradesCts;

        public event EventHandler? BackRequested;
        public event PropertyChangedEventHandler? PropertyChanged;

        // ========= Trades =========
        public ObservableCollection<DiaLastTrade> LastTrades { get; } = new();

        // CollectionView for filtering (LastTrades)
        private ICollectionView? _lastTradesView;

        // Sources for filtering
        public ObservableCollection<string> TradeSources { get; } = new() { "All sources" };

        private string _selectedTradeSource = "All sources";
        public string SelectedTradeSource
        {
            get => _selectedTradeSource;
            set
            {
                if (_selectedTradeSource == value) return;
                _selectedTradeSource = value;
                OnPropertyChanged(nameof(SelectedTradeSource));
                RefreshTradesFilter();
                UpdateMarketStats(); // because stats depend on visible trades (optional)
            }
        }

        private bool _isVerifiedOnly;
        public bool IsVerifiedOnly
        {
            get => _isVerifiedOnly;
            set
            {
                if (_isVerifiedOnly == value) return;
                _isVerifiedOnly = value;
                OnPropertyChanged(nameof(IsVerifiedOnly));
                RefreshTradesFilter();
                UpdateMarketStats();
            }
        }

        // ========= Auto refresh =========
        public ObservableCollection<RefreshPeriodOption> RefreshPeriods { get; } = new()
        {
            new RefreshPeriodOption("5 sec",  TimeSpan.FromSeconds(5)),
            new RefreshPeriodOption("10 sec", TimeSpan.FromSeconds(10)),
            new RefreshPeriodOption("30 sec", TimeSpan.FromSeconds(30)),
            new RefreshPeriodOption("1 min",  TimeSpan.FromMinutes(1)),
        };

        private RefreshPeriodOption? _selectedRefreshPeriod;
        public RefreshPeriodOption? SelectedRefreshPeriod
        {
            get => _selectedRefreshPeriod;
            set
            {
                _selectedRefreshPeriod = value;
                OnPropertyChanged(nameof(SelectedRefreshPeriod));
                ApplyTimerInterval();
                UpdateStatusText();
            }
        }

        private bool _isAutoRefreshTradesEnabled;
        public bool IsAutoRefreshTradesEnabled
        {
            get => _isAutoRefreshTradesEnabled;
            set
            {
                _isAutoRefreshTradesEnabled = value;
                OnPropertyChanged(nameof(IsAutoRefreshTradesEnabled));

                if (_isAutoRefreshTradesEnabled) StartAutoRefresh();
                else StopAutoRefresh();
            }
        }

        private int _maxTradeHistory = 1000;
        public int MaxTradeHistory
        {
            get => _maxTradeHistory;
            set { _maxTradeHistory = value; OnPropertyChanged(nameof(MaxTradeHistory)); }
        }

        private bool _isLastTradesLoading;
        public bool IsLastTradesLoading
        {
            get => _isLastTradesLoading;
            set { _isLastTradesLoading = value; OnPropertyChanged(nameof(IsLastTradesLoading)); }
        }

        private string? _lastTradesErrorText;
        public string? LastTradesErrorText
        {
            get => _lastTradesErrorText;
            set
            {
                _lastTradesErrorText = value;
                OnPropertyChanged(nameof(LastTradesErrorText));
                OnPropertyChanged(nameof(LastTradesErrorVisibility));
            }
        }

        public Visibility LastTradesErrorVisibility =>
            string.IsNullOrWhiteSpace(LastTradesErrorText) ? Visibility.Collapsed : Visibility.Visible;

        private string? _lastTradesStatus;
        public string? LastTradesStatus
        {
            get => _lastTradesStatus;
            set { _lastTradesStatus = value; OnPropertyChanged(nameof(LastTradesStatus)); }
        }

        // ========= Right panel: Quotation analytics =========

        public int AssetDecimals
        {
            get
            {
                // best effort: try to infer from first trade tokens if available
                if (DataContext is DiaQuotedAssetRow row)
                {
                    // If you have Decimals directly on row, replace this
                    // e.g. return row.Decimals;
                    var anyToken = LastTrades.FirstOrDefault()?.QuoteToken ?? LastTrades.FirstOrDefault()?.BaseToken;
                    if (anyToken?.Decimals > 0) return anyToken.Decimals;
                }
                return 0;
            }
        }

        public string Change24hText { get; private set; } = "—";
        public string Change24hAbsText { get; private set; } = "—";
        public string LastQuoteUpdateText { get; private set; } = "—";
        public string QuoteSourceText { get; private set; } = "—";
        public string VolumeTrendText { get; private set; } = "—";


        public string Trades15mText { get; private set; } = "—";
        public string BuySellText { get; private set; } = "—";
        public string VerifiedPctText { get; private set; } = "—";
        public string VwapText { get; private set; } = "—";
        public string MinMaxText { get; private set; } = "—";
        public string TopSourceText { get; private set; } = "—";

        private ICollectionView? _cexPairsView;
        public ICollectionView? CexPairsView
        {
            get => _cexPairsView;
            private set { _cexPairsView = value; OnPropertyChanged(nameof(CexPairsView)); }
        }

        public string CexPairsSummaryText { get; private set; } = "—";

        public QuotedAssetDetailsControl(DiaApiClient api)
        {
            InitializeComponent();
            _api = api;

            Loaded += (_, __) =>
            {
                _lastTradesView = CollectionViewSource.GetDefaultView(LastTrades);
                _lastTradesView.Filter = TradesFilter;
                RefreshTradesFilter();
            };

            LastTrades.CollectionChanged += LastTrades_CollectionChanged;

            _lastTradesTimer = new DispatcherTimer(DispatcherPriority.Background);
            _lastTradesTimer.Tick += async (_, __) =>
            {
                if (IsLastTradesLoading) 
                    return; 

                await LoadLastTradesAsync(appendHistory: true);
            };

            SelectedRefreshPeriod = RefreshPeriods.FirstOrDefault(p => p.Interval == TimeSpan.FromSeconds(10))
                                   ?? RefreshPeriods.First();

            DataContextChanged += async (_, __) =>
            {
                BindCexPairsView();
                UpdateQuoteAnalytics();
                OnPropertyChanged(nameof(AssetDecimals));

                if (_isLoaded)
                    await LoadLastTradesAsync(appendHistory: false);
            };

            Unloaded += (_, __) =>
            {
                StopAutoRefresh();
                CancelLastTradesRequest();
            };

            UpdateStatusText();
        }

        private void LastTrades_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateTradeSources();
            UpdateMarketStats();
            OnPropertyChanged(nameof(AssetDecimals));
        }

        protected virtual void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            BindCexPairsView();
            UpdateQuoteAnalytics();
            await LoadLastTradesAsync(appendHistory: false);
        }

        public async void SetAsset(DiaQuotedAssetRow row)
        {
            DataContext = row;

            BindCexPairsView();
            UpdateQuoteAnalytics();

            if (_isLoaded)
                await LoadLastTradesAsync(appendHistory: false);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
            => BackRequested?.Invoke(this, EventArgs.Empty);

        private async void RefreshLastTrades_Click(object sender, RoutedEventArgs e)
            => await LoadLastTradesAsync(appendHistory: true);

        private void ApplyTimerInterval()
        {
            if (SelectedRefreshPeriod == null) return;
            _lastTradesTimer.Interval = SelectedRefreshPeriod.Interval;
        }

        private void StartAutoRefresh()
        {
            ApplyTimerInterval();
            _lastTradesTimer.Start();
            UpdateStatusText();
        }

        private void StopAutoRefresh()
        {
            _lastTradesTimer.Stop();
            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            if (IsAutoRefreshTradesEnabled)
                LastTradesStatus = $"Auto refresh: {SelectedRefreshPeriod?.Label ?? "?"}";
            else
                LastTradesStatus = LastTrades.Count > 0 ? $"{LastTrades.Count:N0} rows" : "Auto refresh: off";
        }

        private void CancelLastTradesRequest()
        {
            try { _lastTradesCts?.Cancel(); } catch { }
            try { _lastTradesCts?.Dispose(); } catch { }
            _lastTradesCts = null;
        }

        public async Task LoadLastTradesAsync(bool appendHistory)
        {
            if (DataContext is not DiaQuotedAssetRow row) return;

            CancelLastTradesRequest();
            _lastTradesCts = new CancellationTokenSource();

            try
            {
                IsLastTradesLoading = true;
                LastTradesErrorText = null;

                var trades = await _api.GetLastTradesAssetAsync(row.Blockchain, row.Address, _lastTradesCts.Token);
                var ordered = trades.OrderByDescending(x => x.Time).ToList();

                if (!appendHistory)
                {
                    LastTrades.Clear();
                    foreach (var t in ordered)
                        LastTrades.Add(t);

                    RefreshTradesFilter();
                    UpdateStatusText();
                    return;
                }

                int added = 0;

                foreach (var t in ordered)
                {
                    if (!ContainsTrade(t))
                    {
                        InsertSortedDesc(t);
                        added++;
                    }
                }

                while (LastTrades.Count > MaxTradeHistory)
                    LastTrades.RemoveAt(LastTrades.Count - 1);

                RefreshTradesFilter();

                LastTradesStatus = added > 0
                    ? $"+{added} new • total {LastTrades.Count:N0}"
                    : $"no new • total {LastTrades.Count:N0}";
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                LastTradesErrorText = ex.Message;
                LastTradesStatus = "Failed";
            }
            finally
            {
                IsLastTradesLoading = false;
                UpdateMarketStats();
                UpdateQuoteAnalytics();
            }
        }

        private bool ContainsTrade(DiaLastTrade t)
        {
            return LastTrades.Any(x =>
                x.Time == t.Time &&
                string.Equals(x.Source, t.Source, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.Pair, t.Pair, StringComparison.OrdinalIgnoreCase) &&
                x.Price.Equals(t.Price) &&
                x.Volume.Equals(t.Volume));
        }

        private void InsertSortedDesc(DiaLastTrade t)
        {
            if (LastTrades.Count == 0)
            {
                LastTrades.Add(t);
                return;
            }

            if (t.Time >= LastTrades[0].Time)
            {
                LastTrades.Insert(0, t);
                return;
            }

            for (int i = 0; i < LastTrades.Count; i++)
            {
                if (t.Time >= LastTrades[i].Time)
                {
                    LastTrades.Insert(i, t);
                    return;
                }
            }

            LastTrades.Add(t);
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not DiaQuotedAssetRow row) return;

            try
            {
                var sym = row.Symbol;
                if (string.IsNullOrWhiteSpace(sym)) return;

                var quote = await _api.GetQuotationBySymbolAsync(sym, CancellationToken.None);
                row.UpdateQuotation(quote);

                var pairs = await _api.GetPairsAssetCexAsync(quote.Blockchain ?? "", quote.Address ?? "", ct: CancellationToken.None);
                row.CexPairs.Clear();
                foreach (var p in pairs)
                    row.CexPairs.Add(p);

                row.RecalcCexCounts();

                BindCexPairsView();
                UpdateQuoteAnalytics();
            }
            catch (Exception ex)
            {
                LastTradesErrorText = ex.Message;
            }
        }


        private bool TradesFilter(object obj)
        {
            if (obj is not DiaLastTrade t) return false;

            if (IsVerifiedOnly && !t.VerifiedPair)
                return false;

            if (!string.IsNullOrWhiteSpace(SelectedTradeSource) &&
                !string.Equals(SelectedTradeSource, "All sources", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(t.Source, SelectedTradeSource, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private void RefreshTradesFilter()
        {
            if (_lastTradesView == null) return;
            _lastTradesView.Refresh();
        }

        private void UpdateTradeSources()
        {
            var current = SelectedTradeSource;

            var sources = LastTrades
                .Select(t => t.Source)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();

            TradeSources.Clear();
            TradeSources.Add("All sources");
            foreach (var s in sources!)
                TradeSources.Add(s!);

            if (TradeSources.Contains(current))
                SelectedTradeSource = current;
            else
                SelectedTradeSource = "All sources";
        }

        private void UpdateQuoteAnalytics()
        {
            if (DataContext is not DiaQuotedAssetRow row || row.Quotation == null)
            {
                Change24hText = "—";
                Change24hAbsText = "—";
                LastQuoteUpdateText = "—";
                QuoteSourceText = "—";
                VolumeTrendText = "—";

                OnPropertyChanged(nameof(Change24hText));
                OnPropertyChanged(nameof(Change24hAbsText));
                OnPropertyChanged(nameof(LastQuoteUpdateText));
                OnPropertyChanged(nameof(QuoteSourceText));
                OnPropertyChanged(nameof(VolumeTrendText));
                OnPropertyChanged(nameof(AssetDecimals));
                return;
            }

            var price = row.Quotation.Price;
            var y = row.Quotation.PriceYesterday;

            if (y != 0)
            {
                var pct = (price - y) / y * 100.0;
                Change24hText = $"{pct:+0.00;-0.00;0.00}%";
            }
            else
            {
                Change24hText = "—";
            }

            Change24hAbsText = y != 0
                ? $"{(price - y):+0.########;-0.########;0.########}"
                : "—";

            var dt = row.Quotation.Time.LocalDateTime;
            LastQuoteUpdateText = dt == default ? "—" : dt.ToString("yyyy-MM-dd HH:mm:ss");

            QuoteSourceText = string.IsNullOrWhiteSpace(row.Quotation.Source) ? "—" : row.Quotation.Source;

            var today = row.Volume;
            var yesterday = row.Quotation.VolumeYesterdayUSD;
            if (yesterday > 0)
            {
                var vpct = (today - yesterday) / yesterday * 100.0;
                VolumeTrendText = $"{vpct:+0.0;-0.0;0.0}%";
            }
            else
            {
                VolumeTrendText = "—";
            }

            OnPropertyChanged(nameof(Change24hText));
            OnPropertyChanged(nameof(Change24hAbsText));
            OnPropertyChanged(nameof(LastQuoteUpdateText));
            OnPropertyChanged(nameof(QuoteSourceText));
            OnPropertyChanged(nameof(VolumeTrendText));
            OnPropertyChanged(nameof(AssetDecimals));
        }


        private void UpdateMarketStats()
        {
            var now = DateTimeOffset.UtcNow;
            var window = TimeSpan.FromMinutes(15);

            var trades = LastTrades
                .Where(t => t.Time >= now - window)
                .Where(t => TradesFilter(t))
                .ToList();

            if (trades.Count == 0)
            {
                Trades15mText = "0";
                BuySellText = "—";
                VerifiedPctText = "—";
                VwapText = "—";
                MinMaxText = "—";
                TopSourceText = "—";
            }
            else
            {
                Trades15mText = trades.Count.ToString("N0");

                var buys = trades.Count(t => t.IsBuy);
                var sells = trades.Count - buys;
                var buyPct = trades.Count > 0 ? (double)buys / trades.Count * 100.0 : 0.0;
                BuySellText = $"{buys:N0} / {sells:N0} ({buyPct:0}% buy)";

                var verified = trades.Count(t => t.VerifiedPair);
                var vPct = (double)verified / trades.Count * 100.0;
                VerifiedPctText = $"{vPct:0}% ({verified:N0}/{trades.Count:N0})";

                var sumVol = trades.Sum(t => t.AbsVolume);
                if (sumVol > 0)
                {
                    var vwap = trades.Sum(t => t.Price * t.AbsVolume) / sumVol;
                    VwapText = vwap.ToString("N8");
                }
                else
                {
                    VwapText = "—";
                }

                var min = trades.Min(t => t.Price);
                var max = trades.Max(t => t.Price);
                MinMaxText = $"{min:N8} / {max:N8}";

                var top = trades
                    .GroupBy(t => (t.Source ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                    .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key)
                    .FirstOrDefault();

                TopSourceText = top == null ? "—" : $"{top.Key} ({top.Count():N0})";
            }

            OnPropertyChanged(nameof(Trades15mText));
            OnPropertyChanged(nameof(BuySellText));
            OnPropertyChanged(nameof(VerifiedPctText));
            OnPropertyChanged(nameof(VwapText));
            OnPropertyChanged(nameof(MinMaxText));
            OnPropertyChanged(nameof(TopSourceText));
        }

        private void BindCexPairsView()
        {
            if (DataContext is not DiaQuotedAssetRow row || row.CexPairs == null)
            {
                CexPairsView = null;
                CexPairsSummaryText = "—";
                OnPropertyChanged(nameof(CexPairsSummaryText));
                return;
            }

            var view = CollectionViewSource.GetDefaultView(row.CexPairs);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(nameof(DiaCexPairsByAssetRow.Volume24h), ListSortDirection.Descending));
            view.SortDescriptions.Add(new SortDescription(nameof(DiaCexPairsByAssetRow.Trades24h), ListSortDirection.Descending));

            CexPairsView = view;

            var total = row.CexPairs.Count;
            var verified = row.CexPairs.Count(p => p.Verified);
            CexPairsSummaryText = $"{verified:N0}/{total:N0} verified";

            OnPropertyChanged(nameof(CexPairsSummaryText));
        }


        private void CopyAddress_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not DiaQuotedAssetRow row) return;
            if (string.IsNullOrWhiteSpace(row.Address)) return;

            try
            {
                System.Windows.Clipboard.SetText(row.Address);
                LastTradesStatus = "Address copied";
            }
            catch
            {

            }
        }

        private void OpenExplorer_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not DiaQuotedAssetRow row) return;
            var url = BuildExplorerUrl(row.Blockchain, row.Address);
            if (url == null)
            {
                LastTradesStatus = "Explorer not available for this chain";
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                LastTradesErrorText = ex.Message;
            }
        }

        private static string? BuildExplorerUrl(string? blockchain, string? address)
        {
            if (string.IsNullOrWhiteSpace(blockchain) || string.IsNullOrWhiteSpace(address))
                return null;

            var b = blockchain.Trim().ToLowerInvariant();
            var a = address.Trim();

            if (b.Contains("ethereum") || b == "eth")
                return $"https://etherscan.io/address/{a}";
            if (b.Contains("polygon") || b.Contains("matic"))
                return $"https://polygonscan.com/address/{a}";
            if (b.Contains("bsc") || b.Contains("binance"))
                return $"https://bscscan.com/address/{a}";
            if (b.Contains("arbitrum"))
                return $"https://arbiscan.io/address/{a}";
            if (b.Contains("optimism"))
                return $"https://optimistic.etherscan.io/address/{a}";
            if (b.Contains("base"))
                return $"https://basescan.org/address/{a}";
            if (b.Contains("fantom"))
                return $"https://ftmscan.com/address/{a}";
            if (b.Contains("avalanche"))
                return $"https://snowtrace.io/address/{a}";

            return null;
        }
    }

    public sealed class RefreshPeriodOption
    {
        public string Label { get; }
        public TimeSpan Interval { get; }

        public RefreshPeriodOption(string label, TimeSpan interval)
        {
            Label = label;
            Interval = interval;
        }

        public override string ToString() => Label;
    }
}

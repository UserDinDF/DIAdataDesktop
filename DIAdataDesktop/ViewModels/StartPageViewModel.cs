using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DIAdataDesktop.Data;
using DIAdataDesktop.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DIAdataDesktop.ViewModels
{
    public partial class StartPageViewModel : ObservableObject
    {
        private readonly QuotedAssetsViewModel _assets;
        private readonly ExchangesViewModel _exchanges;
        private readonly FavoritesRepository _favoritesRepo;
        private readonly RwaViewModel _rwas;

        public ObservableCollection<FavoriteTileVM> FavoriteAssets { get; } = new();
        public ObservableCollection<FavoriteTileVM> FavoriteExchanges { get; } = new();
        public ObservableCollection<FavoriteTileVM> FavoriteRWAs { get; } = new();

        public ObservableCollection<StatTileVM> Stats { get; } = new();

        [ObservableProperty] private string statusText = "Ready";

        public bool HasFavoriteAssets => FavoriteAssets.Count > 0;
        public bool HasFavoriteExchanges => FavoriteExchanges.Count > 0;
        public bool HasFavoriteRWAs => FavoriteRWAs.Count > 0;

        public Action<string>? Navigate { get; set; }

        public StartPageViewModel(QuotedAssetsViewModel assets, ExchangesViewModel exchanges, RwaViewModel rwas)
        {
            _assets = assets;
            _exchanges = exchanges;
            _rwas = rwas;

            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DIAdataDesktop",
                "diadata.local.db");

            _favoritesRepo = new FavoritesRepository(dbPath);

            FavoriteAssets.CollectionChanged += (_, __) => OnPropertyChanged(nameof(HasFavoriteAssets));
            FavoriteExchanges.CollectionChanged += (_, __) => OnPropertyChanged(nameof(HasFavoriteExchanges));
            FavoriteRWAs.CollectionChanged += (_, __) => OnPropertyChanged(nameof(HasFavoriteRWAs));
        }

        [RelayCommand]
        public async Task InitializeAsync()
        {
            try
            {
                StatusText = "Loading favorites...";
                await LoadFavoritesAsync();
                BuildStats(); 
                StatusText = "Ready";
            }
            catch (Exception ex)
            {
                StatusText = "Start page load failed";
                Debug.WriteLine(ex);
            }
        }

        [RelayCommand] private void OpenAssets() => Navigate?.Invoke("QuotedAssets");
        [RelayCommand] private void OpenExchanges() => Navigate?.Invoke("Exchanges");
        [RelayCommand] private void OpenRwas() => Navigate?.Invoke("RWA");

        private async Task LoadFavoritesAsync(CancellationToken ct = default)
        {
            await _favoritesRepo.EnsureCreatedAsync(ct);

            var tokenKeys = await _favoritesRepo.GetKeysAsync("token", ct);
            var exchangeKeys = await _favoritesRepo.GetKeysAsync("exchange", ct);
            var rwaKeys = await _favoritesRepo.GetKeysAsync("rwa", ct);


            var allAssets = _assets.GetAllRowsSnapshot();
            var favAssets = allAssets
                .Where(r => tokenKeys.Contains(FavoritesRepository.MakeTokenKey(r.Blockchain, r.Address)))
                .OrderByDescending(r => r.Volume)
                .Take(12)
                .ToList();

            var allExchanges = _exchanges._all;
            var favExchanges = allExchanges
                .Where(e => exchangeKeys.Contains(FavoritesRepository.MakeExchangeKey(e.Name)))
                .OrderByDescending(e => e.Volume24h)
                .Take(12)
                .ToList();

            var allRwas = _rwas.GetAllRowsSnapshot();
            var favRwas = allRwas
    .Where(r => rwaKeys.Contains(r.FavKey))
    .OrderBy(r => r.TypeLabel)
    .ThenBy(r => r.AppSlug)
    .Take(12)
    .ToList();



            FavoriteAssets.Clear();
            foreach (var r in favAssets)
            {
                FavoriteAssets.Add(new FavoriteTileVM(
                    title: $"{r.Symbol}",
                    subtitle: $"{r.Blockchain}",
                    iconUrl: r.IconUrl,
                    iconPngPath: "", 
                    open: () => OpenAssetDetails(r),
                    toggleFavorite: async () =>
                    {
                        await _assets.ToggleFavorite(r);
                        await LoadFavoritesAsync(ct);
                        BuildStats();
                    }
                ));
            }

            FavoriteExchanges.Clear();
            foreach (var ex in favExchanges)
            {
                FavoriteExchanges.Add(new FavoriteTileVM(
                    title: ex.Name ?? "",
                    subtitle: $"{ex.Type} • {ex.Blockchain}",
                    iconUrl: null,
                    iconPngPath: "",
                    open: () => OpenExchangeSource(ex),
                    toggleFavorite: async () =>
                    {
                        await _exchanges.ToggleFavoriteByName(ex.Name);
                        await LoadFavoritesAsync(ct);
                        BuildStats();
                    }
                ));
            }

            FavoriteRWAs.Clear();
            foreach (var r in favRwas)
            {
                FavoriteRWAs.Add(new FavoriteTileVM(
                    title: r.AppSlug,
                    subtitle: $"{r.TypeLabel} • {r.Name}",
                    iconUrl: null, 
                    iconPngPath: r.IconPngPath,
                    open: () => OpenRwaApp(r),
                    toggleFavorite: async () =>
                    {
                        await _rwas.ToggleFavorite(r);
                        await LoadFavoritesAsync(ct);
                        BuildStats();
                    }
                ));

                OnPropertyChanged(nameof(HasFavoriteAssets));
                OnPropertyChanged(nameof(HasFavoriteExchanges));
                OnPropertyChanged(nameof(HasFavoriteRWAs));
            }
        }

        private void BuildStats()
        {
            var assets = _assets.GetAllRowsSnapshot();
            var exchanges = _exchanges._all;

            // Assets
            var assetsCount = assets.Count;
            var assetsVol = assets.Sum(x => (decimal)x.Volume);
            var topAsset = assets.OrderByDescending(x => x.Volume).FirstOrDefault();

            // Exchanges
            var exCount = exchanges.Count;
            var exVol = exchanges.Sum(x => (decimal)x.Volume24h);
            var topEx = exchanges.OrderByDescending(x => x.Volume24h).FirstOrDefault();

            var loadedPairs = assets.Sum(a => a.CexPairs?.Count ?? 0);

            Stats.Clear();

            Stats.Add(new StatTileVM("Digital Assets", assetsCount.ToString("N0"), "Total assets loaded", "CurrencyUsd"));
            Stats.Add(new StatTileVM("Assets Volume", FormatUsd(assetsVol), "Sum of asset volumes", "ChartLine"));

            if (topAsset != null)
                Stats.Add(new StatTileVM("Top Asset", $"{topAsset.Symbol}", $"Vol: {FormatUsd((decimal)topAsset.Volume)}", "TrophyOutline"));
            else
                Stats.Add(new StatTileVM("Top Asset", "-", "No data yet", "TrophyOutline"));

            Stats.Add(new StatTileVM("Exchanges", exCount.ToString("N0"), "Total exchanges loaded", "Bank"));
            Stats.Add(new StatTileVM("Exchanges Volume", FormatUsd(exVol), "Sum of exchange 24h volume", "ChartBar"));

            if (topEx != null)
                Stats.Add(new StatTileVM("Top Exchange", $"{topEx.Name}", $"24h: {FormatUsd((decimal)topEx.Volume24h)}", "TrophyOutline"));
            else
                Stats.Add(new StatTileVM("Top Exchange", "-", "No data yet", "TrophyOutline"));

            Stats.Add(new StatTileVM("Pairs", loadedPairs.ToString("N0"), "CEX pairs already prefetched", "LinkVariant"));

            Stats.Add(new StatTileVM("Favorites", $"{FavoriteAssets.Count + FavoriteExchanges.Count + FavoriteRWAs.Count}", "Assets + Exchanges + RWAs", "Star"));
        }

        private static string FormatUsd(decimal v)
        {
            var abs = Math.Abs(v);
            if (abs >= 1_000_000_000m) return $"${(v / 1_000_000_000m):0.##}B";
            if (abs >= 1_000_000m) return $"${(v / 1_000_000m):0.##}M";
            if (abs >= 1_000m) return $"${(v / 1_000m):0.##}K";
            return $"${v:0.##}";
        }

        private static void OpenAssetDetails(DiaQuotedAssetRow row)
        {
            var network = Uri.EscapeDataString(row.Blockchain ?? "");
            var address = row.Address ?? "";
            if (string.IsNullOrWhiteSpace(network) || string.IsNullOrWhiteSpace(address)) return;

            var url = $"https://www.diadata.org/app/price/asset/{network}/{address}/";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private static void OpenExchangeSource(DiaExchange ex)
        {
            var name = Uri.EscapeDataString((ex.Name ?? "").Trim());
            if (string.IsNullOrWhiteSpace(name)) return;

            var url = $"https://www.diadata.org/app/source/defi/{name}/";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private static void OpenRwaApp(DiaRwaRow row)
        {
            var url = row.AppUrl; // z.B. https://www.diadata.org/app/rwa/NG/
            if (string.IsNullOrWhiteSpace(url)) return;

            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }

    public sealed class FavoriteTileVM : ObservableObject
    {
        public FavoriteTileVM(string title, string subtitle, string? iconUrl, string? iconPngPath, Action open, Func<Task> toggleFavorite)
        {
            Title = title;
            Subtitle = subtitle;
            IconUrl = iconUrl;
            IconPngPath = iconPngPath;

            OpenCommand = new RelayCommand(open);
            ToggleFavoriteCommand = new AsyncRelayCommand(toggleFavorite);
        }

        public string Title { get; }
        public string Subtitle { get; }
        public string? IconUrl { get; }
        public string? IconPngPath { get; }

        public IRelayCommand OpenCommand { get; }
        public IAsyncRelayCommand ToggleFavoriteCommand { get; }
    }

    public sealed class StatTileVM : ObservableObject
    {
        public StatTileVM(string title, string value, string subtitle, string iconKind)
        {
            Title = title;
            Value = value;
            Subtitle = subtitle;
            IconKind = iconKind;
        }

        public string Title { get; }
        public string Value { get; }
        public string Subtitle { get; }
        public string IconKind { get; }
    }
}

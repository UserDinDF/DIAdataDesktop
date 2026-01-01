using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DIAdataDesktop.Data;
using DIAdataDesktop.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

        public ObservableCollection<FavoriteTileVM> FavoriteAssets { get; } = new();
        public ObservableCollection<FavoriteTileVM> FavoriteExchanges { get; } = new();

        [ObservableProperty] private string statusText = "Ready";

        public bool HasFavoriteAssets => FavoriteAssets.Count > 0;
        public bool HasFavoriteExchanges => FavoriteExchanges.Count > 0;

        public Action<string>? Navigate { get; set; }

        public StartPageViewModel(QuotedAssetsViewModel assets, ExchangesViewModel exchanges)
        {
            _assets = assets;
            _exchanges = exchanges;

            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DIAdataDesktop",
                "diadata.local.db");

            _favoritesRepo = new FavoritesRepository(dbPath);

            FavoriteAssets.CollectionChanged += (_, __) => OnPropertyChanged(nameof(HasFavoriteAssets));
            FavoriteExchanges.CollectionChanged += (_, __) => OnPropertyChanged(nameof(HasFavoriteExchanges));
        }

        [RelayCommand]
        public async Task InitializeAsync()
        {
            try
            {
                StatusText = "Loading favorites...";
                await LoadFavoritesAsync();
                StatusText = "Ready";
            }
            catch (Exception ex)
            {
                StatusText = "Favorites load failed";
                Debug.WriteLine(ex);
            }
        }

        [RelayCommand]
        private void OpenAssets()
        {
            Navigate?.Invoke("assets");
        }

        [RelayCommand]
        private void OpenExchanges()
        {
            Navigate?.Invoke("exchanges");
        }

        private async Task LoadFavoritesAsync(CancellationToken ct = default)
        {
            await _favoritesRepo.EnsureCreatedAsync(ct);

            var tokenKeys = await _favoritesRepo.GetKeysAsync("token", ct);
            var exchangeKeys = await _favoritesRepo.GetKeysAsync("exchange", ct);

            var allAssets = _assets.GetAllRowsSnapshot();
            var favAssets = allAssets
                .Where(r => tokenKeys.Contains(FavoritesRepository.MakeTokenKey(r.Blockchain, r.Address)))
                .OrderByDescending(r => r.Volume)
                .Take(12)
                .ToList();

            var allExchanges = _exchanges._all;
            var favExchanges = allExchanges
                .Where(e => e.IsFavorite)
                .OrderByDescending(e => e.Volume24h)
                .Take(12)
                .ToList();

            FavoriteAssets.Clear();
            foreach (var r in favAssets)
            {
                FavoriteAssets.Add(new FavoriteTileVM(
                    title: $"{r.Symbol}",
                    subtitle: $"{r.Blockchain}",
                    iconUrl: r.IconUrl,
                    open: () => OpenAssetDetails(r),
                    toggleFavorite: async () =>
                    {
                        await _assets.ToggleFavorite(r);
                        await LoadFavoritesAsync(ct);
                        OnPropertyChanged(nameof(HasFavoriteAssets));
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
                    open: () => OpenExchangeSource(ex),
                    toggleFavorite: async () =>
                    {
                        await _exchanges.ToggleFavoriteByName(ex.Name);
                        await LoadFavoritesAsync(ct);
                        OnPropertyChanged(nameof(HasFavoriteExchanges));
                    }
                ));
            }

            OnPropertyChanged(nameof(HasFavoriteAssets));
            OnPropertyChanged(nameof(HasFavoriteExchanges));
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
            // you said: https://www.diadata.org/app/source/defi/Binance/
            var name = Uri.EscapeDataString((ex.Name ?? "").Trim());
            if (string.IsNullOrWhiteSpace(name)) return;

            var url = $"https://www.diadata.org/app/source/defi/{name}/";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }

    public sealed class FavoriteTileVM : ObservableObject
    {
        public FavoriteTileVM(string title, string subtitle, string? iconUrl, Action open, Func<Task> toggleFavorite)
        {
            Title = title;
            Subtitle = subtitle;
            IconUrl = iconUrl;

            OpenCommand = new RelayCommand(open);
            ToggleFavoriteCommand = new AsyncRelayCommand(toggleFavorite);
        }

        public string Title { get; }
        public string Subtitle { get; }
        public string? IconUrl { get; }

        public IRelayCommand OpenCommand { get; }
        public IAsyncRelayCommand ToggleFavoriteCommand { get; }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using DIAdataDesktop.Models.Enums;
using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace DIAdataDesktop.Models
{
    public partial class DiaRwaRow : ObservableObject
    {
        public DiaRwaRow(
            RwaType type,
            string appSlug,
            string apiTicker,
            string name)
        {
            Type = type;
            AppSlug = appSlug;
            ApiTicker = apiTicker;
            Name = name;

            AppUrl = $"https://www.diadata.org/app/rwa/{AppSlug}/";
            ApiUrl = BuildApiUrl(type, apiTicker);

            IconPngPath = LoadIconFromStartup(appSlug);

            Badge = appSlug; 
        }

        public RwaType Type { get; }
        public string AppSlug { get; }        
        public string ApiTicker { get; }     
        public string Name { get; }

        public string Badge { get; set; } = "";

        // ✅ required by you:
        public string AppUrl { get; }
        public string ApiUrl { get; }

        // PNG icon
        public string IconPngPath { get; set; }

        [ObservableProperty] private decimal price;
        [ObservableProperty] private DateTimeOffset timestamp;

        [ObservableProperty] private bool isFavorite;

        // bottom labels in the screenshot
        public string ExchangeDisplay => TypeLabel;
        public string TypeDisplay => TypeLabel;

        // for search/filter display
        public string Ticker => AppSlug; // show short ticker like "NG" on card

        public string FavKey => $"rwa|{Type}|{AppSlug}".ToLowerInvariant();

        public void ApplyQuote(DiaRwaQuote q)
        {
            Price = q.Price;
            Timestamp = q.Timestamp;
        }

        public string TypeLabel => Type switch
        {
            RwaType.Forex => "Forex",
            RwaType.Commodities => "Commodity",
            RwaType.Etf => "ETF",
            RwaType.Equities => "Equities",
            _ => Type.ToString()
        };

        private static string LoadIconFromStartup(string appSlug)
        {
            var path = AppConfig.AppPaths.RwaIconPath(appSlug);

            if (!File.Exists(path))
                return null;

            return path;
        }

        private static string BuildApiUrl(RwaType type, string apiTicker)
        {
            var baseUrl = "https://api.diadata.org/v1/rwa/";
            return type switch
            {
                RwaType.Forex => $"{baseUrl}Fiat/{apiTicker}",
                RwaType.Commodities => $"{baseUrl}Commodities/{apiTicker}",
                RwaType.Etf => $"{baseUrl}ETF/{apiTicker}",
                RwaType.Equities => $"{baseUrl}Equities/{apiTicker}",
                _ => $"{baseUrl}{apiTicker}"
            };
        }
    }
}

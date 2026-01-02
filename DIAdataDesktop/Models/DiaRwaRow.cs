using CommunityToolkit.Mvvm.ComponentModel;
using DIAdataDesktop.Models.Enums;
using System;
using System.IO;

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

        public string AppUrl { get; }
        public string ApiUrl { get; }

        public string? IconPngPath { get; }

        [ObservableProperty]
        private decimal price;

        [ObservableProperty]
        private DateTimeOffset timestamp;

        [ObservableProperty]
        private bool isFavorite;

        public string ExchangeDisplay => TypeLabel;
        public string TypeDisplay => TypeLabel;

        public string Ticker => AppSlug;

        public string FavKey => $"rwa|{Type}|{AppSlug}".ToLowerInvariant();

        public void ApplyQuote(DiaRwaQuote q)
        {
            if (q == null) return;
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

        private static string? LoadIconFromStartup(string appSlug)
        {
            var path = AppConfig.AppPaths.RwaIconPath(appSlug);
            return File.Exists(path) ? path : null;
        }

        private static string BuildApiUrl(RwaType type, string apiTicker)
        {
            const string baseUrl = "https://api.diadata.org/v1/rwa/";
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

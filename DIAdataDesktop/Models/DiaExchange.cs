using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace DIAdataDesktop.Models
{
    public sealed partial class DiaExchange : ObservableObject
    {
        public string? Name { get; set; }
        public double Volume24h { get; set; }
        public long Trades { get; set; }
        public int Pairs { get; set; }
        public string? Type { get; set; }
        public string? Blockchain { get; set; }
        public bool ScraperActive { get; set; }
        public Uri? LogoSvgPath { get; set; }

        [ObservableProperty] private bool isFavorite;

        public string FavKey => $"exchange|{(Name ?? "").Trim().ToLowerInvariant()}";
    }
}

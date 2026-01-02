using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace DIAdataDesktop.Models
{
    public class RwaWatchlistRowVM : ObservableObject
    {
        public DiaRwaRow Row { get; }

        public RwaWatchlistRowVM(DiaRwaRow row) => Row = row;

        public string? IconPath => Row.IconPngPath;
        public string Ticker => Row.Ticker;
        public string Name => Row.Name;
        public string Type => Row.TypeLabel;

        public decimal Price => Row.Price;
        public DateTime UpdatedTime => Row.Timestamp.LocalDateTime;

        public bool IsFavorite
        {
            get => Row.IsFavorite;
            set => Row.IsFavorite = value;
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace DIAdataDesktop.Models
{
    public class WatchlistRowVM : ObservableObject
    {
        public DiaQuotedAssetRow Row { get; }

        public WatchlistRowVM(DiaQuotedAssetRow row) => Row = row;

        public string? IconUrl => Row.IconUrl;
        public string? Symbol => Row.Symbol;
        public string? Blockchain => Row.Blockchain;

        public decimal Price => (decimal)Row.Price;

        public decimal PriceYesterday => Row.Quotation is null ? 0m : (decimal)Row.Quotation.PriceYesterday;
        public decimal VolumeYesterdayUsd => Row.Quotation is null ? 0m : (decimal)Row.Quotation.VolumeYesterdayUSD;

        public DateTime UpdatedTime => Row.Time.DateTime;

        public decimal Change => Price - PriceYesterday;

        public decimal ChangePct => PriceYesterday <= 0 ? 0 : (Change / PriceYesterday) * 100m;

        public bool IsFavorite
        {
            get => Row.IsFavorite;
            set => Row.IsFavorite = value;
        }
    }
}

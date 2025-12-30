using CommunityToolkit.Mvvm.ComponentModel;
using DIAdataDesktop.Models;
using System;

namespace DIAdataDesktop.Models
{
    public partial class DiaQuotedAssetRow : ObservableObject
    {
        public DiaQuotedAssetRow(DiaQuotedAsset src)
        {
            Asset = src.DiaAsset ?? new DiaAsset();
            Volume = src.Volume;
            VolumeUSD = src.VolumeUSD;
            Index = src.Index;

            Quotation = src.DiaQuotation;
        }

        [ObservableProperty] private DiaAsset asset = new DiaAsset();

        [ObservableProperty] private DiaQuotation? quotation;

        [ObservableProperty] private double volume;
        [ObservableProperty] private double volumeUSD;
        [ObservableProperty] private int index;

        public string Symbol => Quotation?.Symbol ?? Asset?.Symbol ?? "";
        public string Name => Quotation?.Name ?? Asset?.Name ?? "";
        public string Address => Quotation?.Address ?? Asset?.Address ?? "";
        public string Blockchain => Quotation?.Blockchain ?? Asset?.Blockchain ?? "";
        public double Price => Quotation?.Price ?? 0d;
        public DateTimeOffset Time => Quotation?.Time ?? default;

        partial void OnQuotationChanged(DiaQuotation? value)
        {
            OnPropertyChanged(nameof(Symbol));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Address));
            OnPropertyChanged(nameof(Blockchain));
            OnPropertyChanged(nameof(Price));
            OnPropertyChanged(nameof(Time));
        }

        partial void OnAssetChanged(DiaAsset value)
        {
            OnPropertyChanged(nameof(Symbol));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Address));
            OnPropertyChanged(nameof(Blockchain));
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;

namespace DIAdataDesktop.Models
{
    public sealed class WatchlistRowVM : ObservableObject
    {
        public DiaQuotedAssetRow Row { get; }

        public WatchlistRowVM(DiaQuotedAssetRow row)
        {
            Row = row ?? throw new ArgumentNullException(nameof(row));

            Row.PropertyChanged += Row_PropertyChanged;

            if (Row.Quotation is INotifyPropertyChanged q)
                q.PropertyChanged += Quotation_PropertyChanged;
        }

        public string? IconUrl => Row.IconUrl;
        public string? Symbol => Row.Symbol;
        public string? Blockchain => Row.Blockchain;

        public decimal Price => (decimal)Row.Price;

        public decimal PriceYesterday => Row.Quotation is null ? 0m : (decimal)Row.Quotation.PriceYesterday;
        public decimal VolumeYesterdayUsd => Row.Quotation is null ? 0m : (decimal)Row.Quotation.VolumeYesterdayUSD;

        public DateTime UpdatedTime => Row.Time.DateTime;

        public decimal Change => Price - PriceYesterday;

        public decimal ChangePct => PriceYesterday <= 0 ? 0m : (Change / PriceYesterday) * 100m;

        public bool IsFavorite
        {
            get => Row.IsFavorite;
            set
            {
                if (Row.IsFavorite == value) return;
                Row.IsFavorite = value;
                OnPropertyChanged(nameof(IsFavorite));
            }
        }

        private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(DiaQuotedAssetRow.IconUrl):
                    OnPropertyChanged(nameof(IconUrl));
                    break;

                case nameof(DiaQuotedAssetRow.Symbol):
                    OnPropertyChanged(nameof(Symbol));
                    break;

                case nameof(DiaQuotedAssetRow.Blockchain):
                    OnPropertyChanged(nameof(Blockchain));
                    break;

                case nameof(DiaQuotedAssetRow.Price):
                    OnPropertyChanged(nameof(Price));
                    OnPropertyChanged(nameof(Change));
                    OnPropertyChanged(nameof(ChangePct));
                    break;

                case nameof(DiaQuotedAssetRow.Time):
                    OnPropertyChanged(nameof(UpdatedTime));
                    break;

                case nameof(DiaQuotedAssetRow.IsFavorite):
                    OnPropertyChanged(nameof(IsFavorite));
                    break;

                case nameof(DiaQuotedAssetRow.Quotation):
                    HookQuotation(Row.Quotation);
                    RaiseQuotationDependent();
                    break;
            }
        }

        private void HookQuotation(object? quotation)
        {
            if (quotation is INotifyPropertyChanged q)
                q.PropertyChanged += Quotation_PropertyChanged;
        }

        private void Quotation_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RaiseQuotationDependent();
        }

        private void RaiseQuotationDependent()
        {
            OnPropertyChanged(nameof(PriceYesterday));
            OnPropertyChanged(nameof(VolumeYesterdayUsd));
            OnPropertyChanged(nameof(Change));
            OnPropertyChanged(nameof(ChangePct));
        }
    }
}

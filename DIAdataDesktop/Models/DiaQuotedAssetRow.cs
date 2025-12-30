using CommunityToolkit.Mvvm.ComponentModel;
using DIAdataDesktop.Models;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

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

            // ensure not null + hook events
            CexPairs = new ObservableCollection<DiaCexPairsByAssetRow>();
            HookCexPairs(CexPairs);
            RecalcCexCounts();
        }

        public string IconUrl
        {
            get
            {
                var sym = (Symbol ?? "").Trim().ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(sym)) return "";
                return $"https://cms3.diadata.org/images/assets/{Uri.EscapeDataString(sym)}.png";
            }
        }

        [ObservableProperty] private DiaAsset asset = new DiaAsset();

        [ObservableProperty] private DiaQuotation? quotation;

        // (optional) if you still need it; otherwise remove
        [ObservableProperty] private DiaExchange? exchange;

        // IMPORTANT: not null
        [ObservableProperty] private ObservableCollection<DiaCexPairsByAssetRow> cexPairs = new();

        [ObservableProperty] private double volume;
        [ObservableProperty] private double volumeUSD;
        [ObservableProperty] private int index;

        // ✅ Counts as int
        [ObservableProperty] private int cexPairsTotal;
        [ObservableProperty] private int cexPairsVerified;
        [ObservableProperty] private int cexExchangesCount;
        [ObservableProperty] private int cexPairsByExchangeMax;

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
            OnPropertyChanged(nameof(IconUrl)); // ✅
        }

        partial void OnAssetChanged(DiaAsset value)
        {
            OnPropertyChanged(nameof(Symbol));
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Address));
            OnPropertyChanged(nameof(Blockchain));
            OnPropertyChanged(nameof(IconUrl)); // ✅
        }

        partial void OnCexPairsChanged(ObservableCollection<DiaCexPairsByAssetRow> value)
        {
            // unhook old
            if (value == null)
            {
                CexPairs = new ObservableCollection<DiaCexPairsByAssetRow>();
                return;
            }

            HookCexPairs(value);
            RecalcCexCounts();
        }

        private void HookCexPairs(ObservableCollection<DiaCexPairsByAssetRow> col)
        {
            col.CollectionChanged -= CexPairs_CollectionChanged;
            col.CollectionChanged += CexPairs_CollectionChanged;
        }

        private void CexPairs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // quick + safe
            RecalcCexCounts();
        }

        public void RecalcCexCounts()
        {
            var rows = CexPairs ?? new ObservableCollection<DiaCexPairsByAssetRow>();

            CexPairsTotal = rows.Count;
            CexPairsVerified = rows.Count(x => x?.Verified == true);

            CexExchangesCount = rows
                .Select(x => x?.Exchange)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            CexPairsByExchangeMax = rows
                .Where(x => !string.IsNullOrWhiteSpace(x?.Exchange))
                .GroupBy(x => x!.Exchange!, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.Count())
                .DefaultIfEmpty(0)
                .Max();
        }
    }
}

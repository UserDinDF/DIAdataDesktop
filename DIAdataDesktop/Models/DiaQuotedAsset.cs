using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace DIAdataDesktop.Models
{
    public sealed class DiaQuotedAsset : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private DiaAsset _asset = new();
        [JsonPropertyName("Asset")]
        public DiaAsset Asset
        {
            get => _asset;
            set => SetProperty(ref _asset, value ?? new DiaAsset());
        }

        [JsonIgnore]
        public DiaAsset DiaAsset
        {
            get => Asset;
            set => Asset = value ?? new DiaAsset();
        }

        private DiaQuotation? _quotation;
        [JsonPropertyName("Quotation")]
        public DiaQuotation? Quotation
        {
            get => _quotation;
            set => SetProperty(ref _quotation, value);
        }

        [JsonIgnore]
        public DiaQuotation? DiaQuotation
        {
            get => Quotation;
            set => Quotation = value;
        }

        private double _volume;
        [JsonPropertyName("Volume")]
        public double Volume
        {
            get => _volume;
            set => SetProperty(ref _volume, value);
        }

        private double _volumeUSD;
        [JsonPropertyName("VolumeUSD")]
        public double VolumeUSD
        {
            get => _volumeUSD;
            set => SetProperty(ref _volumeUSD, value);
        }

        private int _index;
        [JsonPropertyName("Index")]
        public int Index
        {
            get => _index;
            set => SetProperty(ref _index, value);
        }

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}

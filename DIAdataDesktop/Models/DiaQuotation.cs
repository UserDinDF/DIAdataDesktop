using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DIAdataDesktop.Models
{
    public sealed class DiaQuotation : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string? _symbol;
        public string? Symbol
        {
            get => _symbol;
            set => SetProperty(ref _symbol, value);
        }

        private string? _name;
        public string? Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string? _address;
        public string? Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        private string? _blockchain;
        public string? Blockchain
        {
            get => _blockchain;
            set => SetProperty(ref _blockchain, value);
        }

        private double _price;
        public double Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        private double _priceYesterday;
        public double PriceYesterday
        {
            get => _priceYesterday;
            set => SetProperty(ref _priceYesterday, value);
        }

        private double _volumeYesterdayUSD;
        public double VolumeYesterdayUSD
        {
            get => _volumeYesterdayUSD;
            set => SetProperty(ref _volumeYesterdayUSD, value);
        }

        private DateTimeOffset _time;
        public DateTimeOffset Time
        {
            get => _time;
            set => SetProperty(ref _time, value);
        }

        private string? _source;
        public string? Source
        {
            get => _source;
            set => SetProperty(ref _source, value);
        }

        private string? _signature;
        public string? Signature
        {
            get => _signature;
            set => SetProperty(ref _signature, value);
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

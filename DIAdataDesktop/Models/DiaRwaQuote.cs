using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DIAdataDesktop.Models
{
    public sealed class DiaRwaQuote : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _ticker = "";
        public string Ticker
        {
            get => _ticker;
            set => SetProperty(ref _ticker, value ?? "");
        }

        private decimal _price;
        public decimal Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }

        private DateTimeOffset _timestamp;
        public DateTimeOffset Timestamp
        {
            get => _timestamp;
            set => SetProperty(ref _timestamp, value);
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

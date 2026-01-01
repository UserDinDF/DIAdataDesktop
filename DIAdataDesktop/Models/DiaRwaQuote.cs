using System;
using System.Collections.Generic;
using System.Text;

namespace DIAdataDesktop.Models
{
    public sealed class DiaRwaQuote
    {
        public string Ticker { get; set; } = "";
        public decimal Price { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}

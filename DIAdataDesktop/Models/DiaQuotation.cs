using System;
using System.Collections.Generic;
using System.Text;

namespace DIAdataDesktop.Models
{
    public sealed class DiaQuotation
    {
        public string? Symbol { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? Blockchain { get; set; }
        public double Price { get; set; }
        public double PriceYesterday { get; set; }
        public double VolumeYesterdayUSD { get; set; }
        public DateTimeOffset Time { get; set; }
        public string? Source { get; set; }
        public string? Signature { get; set; }
    }
}

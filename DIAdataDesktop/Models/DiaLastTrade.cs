using System;
using System.Collections.Generic;
using System.Text;

namespace DIAdataDesktop.Models
{
    public sealed class DiaLastTrade
    {
        public string? Symbol { get; set; }
        public string? Pair { get; set; }

        public DiaToken? QuoteToken { get; set; }
        public DiaToken? BaseToken { get; set; }

        public double Price { get; set; }
        public double Volume { get; set; }
        public double EstimatedUSDPrice { get; set; }

        public DateTimeOffset Time { get; set; }

        public string? Source { get; set; }
        public bool VerifiedPair { get; set; }

        public bool IsBuy => Volume > 0;
        public double AbsVolume => Math.Abs(Volume);
    }

}

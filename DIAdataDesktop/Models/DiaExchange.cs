using System;
using System.Collections.Generic;
using System.Text;

namespace DIAdataDesktop.Models
{
    public sealed class DiaExchange
    {
        public string? Name { get; set; }
        public double Volume24h { get; set; }
        public long Trades { get; set; }
        public int Pairs { get; set; }
        public string? Type { get; set; }          // "CEX" / "DEX"
        public string? Blockchain { get; set; }    // z.B. "Ethereum", "" bei CEX
        public bool ScraperActive { get; set; }
    }
}

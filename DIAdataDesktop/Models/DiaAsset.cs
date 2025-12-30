using System;
using System.Collections.Generic;
using System.Text;

namespace DIAdataDesktop.Models
{
    public sealed class DiaAsset
    {
        public string? Symbol { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public int Decimals { get; set; }
        public string? Blockchain { get; set; }
    }
}

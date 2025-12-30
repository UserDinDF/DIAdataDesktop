using System;
using System.Collections.Generic;
using System.Text;

namespace DIAdataDesktop.Models
{
    public sealed class DiaQuotedAsset
    {
        public DiaAsset? Asset { get; set; }
        public double Volume { get; set; }
        public double VolumeUSD { get; set; }
        public int Index { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace DIAdataDesktop.Models
{
    public sealed class FavoriteTokenRecord
    {
        public string Blockchain { get; set; } = "";
        public string Address { get; set; } = "";
        public string Symbol { get; set; } = "";
        public string Name { get; set; } = "";
        public DateTimeOffset AddedAt { get; set; }
    }
}

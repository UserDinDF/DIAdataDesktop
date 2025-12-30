using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DIAdataDesktop.Models
{
    public sealed class DiaCexPairsByAssetRow
    {
        [JsonPropertyName("Symbol")]
        public string? Symbol { get; set; }

        [JsonPropertyName("ForeignName")]
        public string? ForeignName { get; set; }

        // IMPORTANT: DIA returns "EXchange" (weird casing)
        [JsonPropertyName("EXchange")]
        public string? Exchange { get; set; }

        [JsonPropertyName("Verified")]
        public bool Verified { get; set; }

        [JsonPropertyName("UnderlyingPair")]
        public DiaUnderlyingPair? UnderlyingPair { get; set; }
    }

    public sealed class DiaUnderlyingPair
    {
        // In DIA JSON: QuoteToken = requested asset (HYPE), BaseToken = USDT/USDC
        [JsonPropertyName("QuoteToken")]
        public DiaToken? QuoteToken { get; set; }

        [JsonPropertyName("BaseToken")]
        public DiaToken? BaseToken { get; set; }
    }

    public sealed class DiaToken
    {
        [JsonPropertyName("Symbol")]
        public string? Symbol { get; set; }

        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        [JsonPropertyName("Address")]
        public string? Address { get; set; }

        [JsonPropertyName("Decimals")]
        public int Decimals { get; set; }

        [JsonPropertyName("Blockchain")]
        public string? Blockchain { get; set; }
    }
}

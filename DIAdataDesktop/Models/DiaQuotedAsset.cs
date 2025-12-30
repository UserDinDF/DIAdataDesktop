using System.Text.Json.Serialization;

namespace DIAdataDesktop.Models
{
    public sealed class DiaQuotedAsset
    {
        // ✅ JSON liefert "Asset"
        [JsonPropertyName("Asset")]
        public DiaAsset Asset { get; set; } = new();

        // optional: falls du intern noch DiaAsset verwenden willst
        [JsonIgnore]
        public DiaAsset DiaAsset
        {
            get => Asset;
            set => Asset = value ?? new DiaAsset();
        }

        // kommt bei quotedAssets meistens NICHT -> du lädst es extra
        [JsonPropertyName("Quotation")]
        public DiaQuotation? Quotation { get; set; }

        [JsonIgnore]
        public DiaQuotation? DiaQuotation
        {
            get => Quotation;
            set => Quotation = value;
        }

        [JsonPropertyName("Volume")]
        public double Volume { get; set; }

        // nicht immer vorhanden -> bleibt 0, ist ok
        [JsonPropertyName("VolumeUSD")]
        public double VolumeUSD { get; set; }

        [JsonPropertyName("Index")]
        public int Index { get; set; }
    }
}

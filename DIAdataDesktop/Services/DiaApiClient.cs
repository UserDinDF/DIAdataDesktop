using DIAdataDesktop.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DIAdataDesktop.Services
{
    public sealed class DiaApiClient
    {
        private static readonly Uri BaseUri = new("https://api.diadata.org/v1/");
        private readonly HttpClient _http;

        public DiaApiClient(HttpClient? httpClient = null)
        {
            _http = httpClient ?? new HttpClient();
            _http.BaseAddress = BaseUri;
            _http.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task<DiaQuotation> GetQuotationBySymbolAsync(string symbol, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("symbol is null/empty.", nameof(symbol));

            symbol = symbol.Trim().ToUpperInvariant();

            var quote = await _http.GetFromJsonAsync<DiaQuotation>(
                $"quotation/{Uri.EscapeDataString(symbol)}", ct);

            return quote ?? throw new InvalidOperationException("DIA API returned empty response.");
        }

        public async Task<DiaQuotation> GetQuotationByAddressAsync(
            string blockchain,
            string assetAddress,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(blockchain))
                throw new ArgumentException("blockchain is null/empty.", nameof(blockchain));
            if (string.IsNullOrWhiteSpace(assetAddress))
                throw new ArgumentException("assetAddress is null/empty.", nameof(assetAddress));

            blockchain = blockchain.Trim();
            assetAddress = assetAddress.Trim();

            var path =
                $"assetQuotation/{Uri.EscapeDataString(blockchain)}/{Uri.EscapeDataString(assetAddress)}";

            var quote = await _http.GetFromJsonAsync<DiaQuotation>(path, ct);

            return quote ?? throw new InvalidOperationException("DIA API returned empty response.");
        }

        /// <summary>
        /// GET /blockchains
        /// </summary>
        public async Task<List<string>> GetBlockchainsAsync(CancellationToken ct = default)
        {
            var list = await _http.GetFromJsonAsync<List<string>>("blockchains", ct);
            return list ?? new List<string>();
        }

        /// <summary>
        /// GET /exchanges
        /// </summary>
        public async Task<List<DiaExchange>> GetExchangesAsync(CancellationToken ct = default)
        {
            var list = await _http.GetFromJsonAsync<List<DiaExchange>>("exchanges", ct);
            return list ?? new List<DiaExchange>();
        }

        public async Task<List<DiaCexPairsByAssetRow>> GetPairsAssetCexAsync(
          string blockchain,
          string address,
          bool? verified = null,
          CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(blockchain))
                throw new ArgumentException("blockchain is null/empty.", nameof(blockchain));
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("address is null/empty.", nameof(address));

            var path = $"pairsAssetCex/{Uri.EscapeDataString(blockchain.Trim())}/{Uri.EscapeDataString(address.Trim())}";

            if (verified.HasValue)
                path += $"?verified={(verified.Value ? "true" : "false")}";

            var list = await _http.GetFromJsonAsync<List<DiaCexPairsByAssetRow>>(path, ct);
            return list ?? new List<DiaCexPairsByAssetRow>();
        }

        /// <summary>
        /// GET /quotedAssets?blockchain=Polygon
        /// </summary>
        public async Task<List<DiaQuotedAsset>> GetQuotedAssetsAsync(string? blockchain = null, CancellationToken ct = default)
        {
            var path = "quotedAssets";
            if (!string.IsNullOrWhiteSpace(blockchain))
                path += $"?blockchain={Uri.EscapeDataString(blockchain.Trim())}";

            var list = await _http.GetFromJsonAsync<List<DiaQuotedAsset>>(path, ct);
            return list ?? new List<DiaQuotedAsset>();
        }
    }
}

using DIAdataDesktop.Models;
using System;
using System.Net.Http;
using System.Net.Http.Json;
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

        /// <summary>
        /// GET /quotation/{symbol}
        /// </summary>
        public async Task<DiaQuotation> GetQuotationBySymbolAsync(string symbol, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("symbol is null/empty.", nameof(symbol));

            symbol = symbol.Trim().ToUpperInvariant();

            var quote = await _http.GetFromJsonAsync<DiaQuotation>(
                $"quotation/{Uri.EscapeDataString(symbol)}", ct);

            return quote ?? throw new InvalidOperationException("DIA API returned empty response.");
        }

        /// <summary>
        /// GET /assetQuotation/{blockchain}/{asset}
        /// Example: /assetQuotation/Bitcoin/0x0000000000000000000000000000000000000000
        /// </summary>
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
    }
}

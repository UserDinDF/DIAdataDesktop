using DIAdataDesktop.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
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

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        public DiaApiClient(HttpClient? httpClient = null)
        {
            _http = httpClient ?? new HttpClient();
            _http.BaseAddress = BaseUri;
            _http.Timeout = TimeSpan.FromSeconds(10);
        }

        private async Task<T> GetJsonAsync<T>(string relativePath, CancellationToken ct)
        {
             var url = new Uri(_http.BaseAddress!, relativePath);

            var sw = Stopwatch.StartNew();
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, relativePath);

                using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                sw.Stop();

                var body = await resp.Content.ReadAsStringAsync(ct);

                if (!resp.IsSuccessStatusCode)
                {
                    throw new DiaApiException(
                        message: $"DIA API call failed: {(int)resp.StatusCode} {resp.ReasonPhrase}",
                        requestUrl: url.ToString(),
                        statusCode: resp.StatusCode,
                        responseBody: body,
                        elapsedMs: sw.ElapsedMilliseconds);
                }

                if (string.IsNullOrWhiteSpace(body))
                    throw new DiaApiException("DIA API returned empty body.", url.ToString(), resp.StatusCode, body, sw.ElapsedMilliseconds);

                var result = JsonSerializer.Deserialize<T>(body, JsonOptions);
                if (result == null)
                    throw new DiaApiException("DIA API returned null JSON.", url.ToString(), resp.StatusCode, body, sw.ElapsedMilliseconds);

                return result;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                sw.Stop();
                Debug.WriteLine($"[DIA API] CANCELED {url} after {sw.ElapsedMilliseconds}ms");
                throw;
            }
            catch (HttpRequestException ex)
            {
                sw.Stop();
                Debug.WriteLine($"[DIA API] HttpRequestException on {url} after {sw.ElapsedMilliseconds}ms: {ex}");
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                Debug.WriteLine($"[DIA API] Exception on {url} after {sw.ElapsedMilliseconds}ms: {ex}");
                throw;
            }
        }

        public async Task<DiaQuotation> GetQuotationBySymbolAsync(string symbol, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("symbol is null/empty.", nameof(symbol));

            symbol = symbol.Trim().ToUpperInvariant();
            return await GetJsonAsync<DiaQuotation>($"quotation/{Uri.EscapeDataString(symbol)}", ct);
        }

        public async Task<DiaQuotation> GetQuotationByAddressAsync(string blockchain, string assetAddress, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(blockchain))
                throw new ArgumentException("blockchain is null/empty.", nameof(blockchain));
            if (string.IsNullOrWhiteSpace(assetAddress))
                throw new ArgumentException("assetAddress is null/empty.", nameof(assetAddress));

            var path = $"assetQuotation/{Uri.EscapeDataString(blockchain.Trim())}/{Uri.EscapeDataString(assetAddress.Trim())}";
             return await GetJsonAsync<DiaQuotation>(path, ct);
        }

        public async Task<List<string>> GetBlockchainsAsync(CancellationToken ct = default)
            => await GetJsonAsync<List<string>>("blockchains", ct);

        public async Task<List<DiaExchange>> GetExchangesAsync(CancellationToken ct = default)
            => await GetJsonAsync<List<DiaExchange>>("exchanges", ct);

        public async Task<List<DiaCexPairsByAssetRow>> GetPairsCexAsync(string exchange, bool? verified = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(exchange))
                throw new ArgumentException("exchange is null/empty.", nameof(exchange));

            var path = $"pairsCex/{Uri.EscapeDataString(exchange.Trim())}";

            if (verified.HasValue)
                path += $"?verified={(verified.Value ? "true" : "false")}";

            return await GetJsonAsync<List<DiaCexPairsByAssetRow>>(path, ct);
        }

        public async Task<List<DiaCexPairsByAssetRow>> GetPairsAssetCexAsync(string blockchain, string address, bool? verified = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(blockchain))
                throw new ArgumentException("blockchain is null/empty.", nameof(blockchain));
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("address is null/empty.", nameof(address));

            var path = $"pairsAssetCex/{Uri.EscapeDataString(blockchain.Trim())}/{Uri.EscapeDataString(address.Trim())}";
            if (verified.HasValue)
                path += $"?verified={(verified.Value ? "true" : "false")}";

            return await GetJsonAsync<List<DiaCexPairsByAssetRow>>(path, ct);
        }

        public async Task<List<DiaQuotedAsset>> GetQuotedAssetsAsync(string? blockchain = null, CancellationToken ct = default)
        {
            var path = "quotedAssets";
            if (!string.IsNullOrWhiteSpace(blockchain))
                path += $"?blockchain={Uri.EscapeDataString(blockchain.Trim())}";

            return await GetJsonAsync<List<DiaQuotedAsset>>(path, ct);
        }

        public async Task<List<DIAdataDesktop.Models.DiaLastTrade>> GetLastTradesAssetAsync(string blockchain, string address, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(blockchain))
                throw new ArgumentException("blockchain is null/empty.", nameof(blockchain));
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("address is null/empty.", nameof(address));

            var path = $"lastTradesAsset/{Uri.EscapeDataString(blockchain.Trim())}/{Uri.EscapeDataString(address.Trim())}";
            return await GetJsonAsync<List<DIAdataDesktop.Models.DiaLastTrade>>(path, ct);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using CloudflareDDNS.Api.Models;

namespace CloudflareDDNS.Api
{
    public class CloudflareApi : ICloudflareApi
    {
        public const string ENDPOINT = "https://api.cloudflare.com/client/v4";
        private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            IgnoreNullValues = true,
            PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
        };

        private readonly HttpClient _client;
        private readonly CloudflareApiOptions _options;

        public CloudflareApi(HttpClient client, CloudflareApiOptions options)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _client = client;
            _options = options;
        }

        public async Task<Zone> GetZoneDetails(string zoneId)
        {
            var builder = new UriBuilder($"{ENDPOINT}/zones/{zoneId}");

            using var message = new HttpRequestMessage
            {
                RequestUri = builder.Uri,
                Method = HttpMethod.Get,
                Headers = {
                    { HttpRequestHeader.Authorization.ToString(), $"Bearer {_options.ApiToken}" },
                    { HttpRequestHeader.ContentType.ToString(), "application/json" },
                }
            };

            var response = await _client.SendAsync(message);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ApiResult<Zone>>(_serializerOptions);
            return result.Result;
        }

        public async Task<(List<DnsResult> results, ApiResultPager? pager)> ListDNSRecords(string zoneId, string? type = null, string? name = null, int page = 1, int perPage = 20)
        {
            var builder = new UriBuilder($"{ENDPOINT}/zones/{zoneId}/dns_records");
            var queryString = HttpUtility.ParseQueryString(builder.Query);

            queryString.Add("type", type);
            queryString.Add("name", name);
            queryString.Add("page", $"{page}");
            queryString.Add("per_page", $"{perPage}");

            builder.Query = queryString.ToString();

            using var message = new HttpRequestMessage
            {
                RequestUri = builder.Uri,
                Method = HttpMethod.Get,
                Headers = {
                    { HttpRequestHeader.Authorization.ToString(), $"Bearer {_options.ApiToken}" },
                    { HttpRequestHeader.ContentType.ToString(), "application/json" },
                }
            };

            var response = await _client.SendAsync(message);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ApiResult<List<DnsResult>>>(_serializerOptions);
            return (
                results: result.Result is null ? new List<DnsResult>() : result.Result,
                pager: result.Pager
            );
        }

        public async Task<DnsResult> CreateDNSRecord(string zoneId, string type, string name, string content, long? ttl = null, bool? proxied = null)
        {
            var builder = new UriBuilder($"{ENDPOINT}/zones/{zoneId}/dns_records");
            var dns = new
            {
                Type = type,
                Name = name,
                Content = content,
                Ttl = ttl,
                Proxied = proxied,
            };

            using var message = new HttpRequestMessage
            {
                RequestUri = builder.Uri,
                Method = HttpMethod.Post,
                Headers = {
                    { HttpRequestHeader.Authorization.ToString(), $"Bearer {_options.ApiToken}" },
                    { HttpRequestHeader.ContentType.ToString(), "application/json" }
                },
                Content = JsonContent.Create(dns, new MediaTypeHeaderValue("application/json"), _serializerOptions)
            };

            var response = await _client.SendAsync(message);
            var result = await response.Content.ReadFromJsonAsync<ApiResult<DnsResult>>(_serializerOptions);

            if (!result.Success)
            {
                var error = result.Errors.FirstOrDefault() ?? ApiError.Unknown;
                throw new ApiException(error.Code, error.Message, response.StatusCode);
            }

            return result.Result;
        }

        public async Task<DnsResult> UpdateDNSRecord(string zoneId, string id, string type, string name, string content, long? ttl = null, bool? proxied = null)
        {
            var builder = new UriBuilder($"{ENDPOINT}/zones/{zoneId}/dns_records/{id}");
            var dns = new
            {
                Type = type,
                Name = name,
                Content = content,
                Ttl = ttl,
                Proxied = proxied,
            };

            using var message = new HttpRequestMessage
            {
                RequestUri = builder.Uri,
                Method = HttpMethod.Put,
                Headers = {
                        { HttpRequestHeader.Authorization.ToString(), $"Bearer {_options.ApiToken}" },
                        { HttpRequestHeader.ContentType.ToString(), "application/json" }
                    },
                Content = JsonContent.Create(dns, new MediaTypeHeaderValue("application/json"), _serializerOptions)
            };

            var response = await _client.SendAsync(message);
            var result = await response.Content.ReadFromJsonAsync<ApiResult<DnsResult>>(_serializerOptions);

            if (!result.Success)
            {
                var error = result.Errors.FirstOrDefault() ?? ApiError.Unknown;
                throw new ApiException(error.Code, error.Message, response.StatusCode);
            }

            return result.Result;
        }
    }
}

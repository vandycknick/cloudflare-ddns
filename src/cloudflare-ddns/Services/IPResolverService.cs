using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;

namespace CloudflareDDNS.Service
{
    public class IPResolverServiceOptions
    {
        public IPResolverServiceOptions(string ipv4Resolver, string ipv6Resolver)
        {
            IPv4Resolver = ipv4Resolver;
            IPv6Resolver = ipv6Resolver;
        }
        public string IPv4Resolver { get; set; }
        public string IPv6Resolver { get; set; }
    }

    public class IPResolverService
    {
        private readonly HttpClient _client;
        private readonly ILogger _logger;
        private readonly IPResolverServiceOptions _options;

        public IPResolverService(HttpClient client, ILogger logger, IPResolverServiceOptions options)
        {
            if (client is null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _client = client;
            _logger = logger;
            _options = options;
        }

        private async Task<IPAddress> ResolveIP(string endpoint)
        {
            var response = await _client.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            return IPAddress.Parse(responseBody);
        }

        public async Task<List<IPAddress>> Resolve()
        {
            var addresses = new List<IPAddress>();
            var endpoints = new List<string>
            {
                _options.IPv4Resolver,
                _options.IPv6Resolver,
            };

            foreach (var endpoint in endpoints)
            {
                try
                {
                    var ip = await ResolveIP(endpoint);
                    addresses.Add(ip);
                }
                catch (Exception ex)
                {
                    _logger.Verbose("Error resolving ip from {endpoint}: {message}", endpoint, ex.Message);
                }
            }

            return addresses;
        }

    }
}

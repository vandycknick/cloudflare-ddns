using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;

namespace CloudflareDDNS.Service
{
    public class PublicIPResolverOverHttp : IPublicIPResolver
    {
        private readonly HttpClient _client;
        private readonly ILogger _logger;
        private readonly PublicIPResolverOverHttpOptions _options;

        public PublicIPResolverOverHttp(HttpClient client, ILogger logger, PublicIPResolverOverHttpOptions options)
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

        private async Task<IPAddress?> ResolveIP(string endpoint)
        {
            try
            {
                var response = await _client.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();

                return IPAddress.Parse(responseBody);
            }
            catch (Exception ex)
            {
                var type = endpoint == _options.IPv4Endpoint ? "ipv4" : "ipv6";
                _logger.Verbose("Can't resolve {type} from {endpoint}: {message}", type, endpoint, ex.Message);
                return null;
            }
        }

        public Task<IPAddress?> ResolveIPv4() => ResolveIP(_options.IPv4Endpoint);

        public Task<IPAddress?> ResolveIPv6() => ResolveIP(_options.IPv6Endpoint);
    }
}

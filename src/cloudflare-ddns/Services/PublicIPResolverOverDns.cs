using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using DnsClient;
using Serilog;

namespace CloudflareDDNS.Service
{
    public class PublicIPResolverOverDns : IPublicIPResolver
    {
        public static PublicIPResolverOverDnsOptions GetOptionsForDnsServer(string name) =>
            name switch
            {
                "cloudflare" => new PublicIPResolverOverDnsOptions(
                    query: "whoami.cloudflare",
                    isTxt: true,
                    ipv4NameServers: new List<IPAddress>
                    {
                        IPAddress.Parse("1.1.1.1"),
                        IPAddress.Parse("1.0.0.1"),
                    },
                    ipv6NameServers: new List<IPAddress>
                    {
                        IPAddress.Parse("2606:4700:4700::1111"),
                        IPAddress.Parse("2606:4700:4700::1001"),
                    }
                ),
                "google" => new PublicIPResolverOverDnsOptions(
                    query: "o-o.myaddr.l.google.com",
                    isTxt: true,
                    ipv4NameServers: new List<IPAddress>
                    {
                        IPAddress.Parse("216.239.32.10"),
                        IPAddress.Parse("216.239.34.10"),
                        IPAddress.Parse("216.239.36.10"),
                        IPAddress.Parse("216.239.38.10"),
                    },
                    ipv6NameServers: new List<IPAddress>
                    {
                        IPAddress.Parse("2001:4860:4802:32::a"),
                        IPAddress.Parse("2001:4860:4802:34::a"),
                        IPAddress.Parse("2001:4860:4802:36::a"),
                        IPAddress.Parse("2001:4860:4802:38::a"),
                    }
                ),
                _ => throw new Exception("Unknown server!")
            };

        private readonly ILookupClient _client;
        private readonly ILogger _logger;
        private readonly PublicIPResolverOverDnsOptions _options;
        private readonly DnsQueryAndServerOptions _ipv4ServerOptions;
        private readonly DnsQueryAndServerOptions _ipv6ServerOptions;

        public PublicIPResolverOverDns(ILookupClient client, ILogger logger, PublicIPResolverOverDnsOptions options)
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

            _ipv4ServerOptions = new DnsQueryAndServerOptions(_options.IPv4NameServers.ToArray())
            {
                UseCache = false,
            };
            _ipv6ServerOptions = new DnsQueryAndServerOptions(_options.IPv6NameServers.ToArray())
            {
                UseCache = false,
            };
        }

        private async Task<IPAddress?> Query(string query, DnsQueryAndServerOptions options)
        {
            try
            {
                var question = new DnsQuestion(query, QueryType.TXT);
                var response = await _client.QueryAsync(question, options);
                var ip = response.Answers.TxtRecords().FirstOrDefault()?.Text.FirstOrDefault();

                if (!string.IsNullOrEmpty(ip) && IPAddress.TryParse(ip, out var address))
                {
                    return address;
                }

                return null;
            }
            catch (Exception ex)
            {
                var type = options == _ipv4ServerOptions ? "ipv4" : "ipv6";
                _logger.Verbose("Error resolving {ip} over dns: {message}", type, ex.Message);
                return null;
            }
        }

        public async Task<IPAddress?> ResolveIPv4()
        {
            var ip = await Query(_options.Query, _ipv4ServerOptions);

            if (ip?.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip;
            }

            return null;
        }

        public async Task<IPAddress?> ResolveIPv6()
        {
            var ip = await Query(_options.Query, _ipv6ServerOptions);

            if (ip?.AddressFamily == AddressFamily.InterNetworkV6)
            {
                return ip;
            }

            return null;
        }
    }
}

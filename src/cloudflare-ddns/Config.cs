using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CloudflareDDNS
{
    public class Config
    {
        public const string ENV_CLOUDFLARE_API_TOKEN = "CLOUDFLARE_API_TOKEN";
        private static readonly List<string> SUPPORTED_DNS_SERVERS = new()
        {
            "cloudflare",
            "google"
        };

        public static Task<Config> LoadFromAsync(string path) => LoadFromAsync(path, new FileSystem());


        public static async Task<Config> LoadFromAsync(string path, IFileSystem fileSystem)
        {
            using var stream = fileSystem.File.OpenRead(path);
            var config = await JsonSerializer.DeserializeAsync<Config>(stream);
            var apiToken = Environment.GetEnvironmentVariable(ENV_CLOUDFLARE_API_TOKEN);

            if (config == null)
            {
                throw new Exception("Config can not be null!");
            }

            config.ApiToken = apiToken ?? config.ApiToken;

            if (string.IsNullOrEmpty(config.ApiToken))
            {
                throw new InvalidConfigException(nameof(config.ApiToken), $"Property '{nameof(config.ApiToken)}' can't be null or empty!");
            }

            if (!SUPPORTED_DNS_SERVERS.Contains(config.Resolvers.DnsServer))
            {
                throw new InvalidConfigException(nameof(config.Resolvers.DnsServer), $"Property '{nameof(config.Resolvers.DnsServer)}' can only be 'cloudflare' or 'google'!");
            }

            foreach (var item in config.Records)
            {
                if (string.IsNullOrEmpty(item.ZoneId))
                {
                    throw new InvalidConfigException(nameof(item.ZoneId), $"Property '{nameof(item.ZoneId)}' can't be null or empty!");
                }

                if (string.IsNullOrEmpty(item.Subdomain))
                {
                    throw new InvalidConfigException(nameof(item.Subdomain), $"Property '{nameof(item.Subdomain)}' can't be null or empty!");
                }
            }

            return config;
        }

        [JsonPropertyName("apiToken")]
        public string ApiToken { get; set; } = null!;

        [JsonPropertyName("ipv4")]
        public bool IPv4 { get; set; } = true;

        [JsonPropertyName("ipv6")]
        public bool IPv6 { get; set; } = true;

        [JsonPropertyName("resolvers")]
        public ResolversConfig Resolvers { get; set; } = new ResolversConfig();

        [JsonPropertyName("records")]
        public List<RecordsConfig> Records { get; set; } = new List<RecordsConfig>();
    }

    public class ResolversConfig
    {
        [JsonPropertyName("http")]
        public HttpResolverConfig Http { get; set; } = new HttpResolverConfig
        {
            IPv4Endpoint = "https://api.ipify.org",
            IPv6Endpoint = "https://api6.ipify.org",
        };

        [JsonPropertyName("dns")]
        public string DnsServer { get; set; } = "cloudflare";

        [JsonPropertyName("order")]
        public List<string> Order { get; set; } = new List<string>()
        {
            "dns",
            "http",
        };
    }

    public class HttpResolverConfig
    {
        [JsonPropertyName("ipv4Endpoint")]
        public string IPv4Endpoint { get; set; } = null!;

        [JsonPropertyName("ipv6Endpoint")]
        public string IPv6Endpoint { get; set; } = null!;
    }

    public class RecordsConfig
    {
        [JsonPropertyName("zoneId")]
        public string ZoneId { get; set; } = null!;

        [JsonPropertyName("subdomain")]
        public string Subdomain { get; set; } = null!;

        [JsonPropertyName("proxied")]
        public bool Proxied { get; set; } = false;
    }

    public class InvalidConfigException : Exception
    {
        public InvalidConfigException(string property, string message) : base(message)
        {
            Property = property;
        }

        public string Property { get; set; }
    }
}

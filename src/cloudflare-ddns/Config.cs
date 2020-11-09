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
        public static Task<Config> LoadFromAsync(string path) => LoadFromAsync(path, new FileSystem());

        public static async Task<Config> LoadFromAsync(string path, IFileSystem fileSystem)
        {
            using var stream = fileSystem.File.OpenRead(path);
            var config = await JsonSerializer.DeserializeAsync<Config>(stream);

            if (string.IsNullOrEmpty(config.ApiKey))
            {
                throw new InvalidConfigException(nameof(config.ApiKey), "Property can't be null or empty!");
            }

            foreach (var item in config.Dns)
            {
                if (string.IsNullOrEmpty(item.ZoneId))
                {
                    throw new InvalidConfigException(nameof(item.ZoneId), "Property can't be null or empty!");
                }

                if (string.IsNullOrEmpty(item.Domain))
                {
                    throw new InvalidConfigException(nameof(item.Domain), "Property can't be null or empty!");
                }
            }

            return config;
        }

        [JsonPropertyName("ipv4Resolver")]
        public string IPv4Resolver { get; set; } = "https://api.ipify.org";

        [JsonPropertyName("ipv6Resolver")]
        public string IPv6Resolver { get; set; } = "https://api6.ipify.org";

        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; } = null!;

        [JsonPropertyName("dns")]
        public List<DnsConfig> Dns { get; set; } = new List<DnsConfig>();
    }

    public class DnsConfig
    {
        [JsonPropertyName("zoneId")]
        public string ZoneId { get; set; } = null!;

        [JsonPropertyName("domain")]
        public string Domain { get; set; } = null!;

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

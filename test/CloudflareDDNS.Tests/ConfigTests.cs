using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CloudflareDDNS.Tests
{
    public class ConfigTests
    {
        private readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions
        {
            IgnoreNullValues = false,
        };

        public ConfigTests()
        {
            Environment.SetEnvironmentVariable(Config.ENV_CLOUDFLARE_API_TOKEN, null);
        }

        [Fact]
        public async Task Config_LoadFrom_ParsesAMinimalValidConfigFile()
        {
            // Given
            var contents = JsonSerializer.Serialize(
                new
                {
                    apiToken = "123",
                    records = new[]
                    {
                        new { zoneId = "456", subdomain = "test" }
                    }
                },
                serializerOptions
            );
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"~/config.json", new MockFileData(contents) },
            });

            // When
            var config = await Config.LoadFromAsync("~/config.json", fileSystem);

            // Then
            Assert.Equal("123", config.ApiToken);
            Assert.True(config.IPv4);
            Assert.True(config.IPv6);
            config.Resolvers.Http.Should().BeEquivalentTo(new HttpResolverConfig
            {
                IPv4Endpoint = "https://api.ipify.org",
                IPv6Endpoint = "https://api6.ipify.org",
            });
            Assert.Equal("cloudflare", config.Resolvers.DnsServer);
            Assert.Collection(
                config.Resolvers.Order,
                o => Assert.Equal("dns", o),
                o => Assert.Equal("http", o)
            );
            Assert.Single(config.Records);
            Assert.Collection(
                config.Records,
                item => item.Should().BeEquivalentTo(new RecordsConfig
                {
                    ZoneId = "456",
                    Subdomain = "test",
                    Proxied = false,
                })
            );
        }

        [Fact]
        public async Task Config_LoadFrom_ParsesAGivenConfigFileWithCustomHttpResolvers()
        {
            // Given
            var contents = JsonSerializer.Serialize(
                new
                {
                    apiToken = "123",
                    resolvers = new
                    {
                        http = new
                        {
                            ipv4Endpoint = "https://custom-ipv4.org",
                            ipv6Endpoint = "https://custom-ipv6.org"
                        }
                    },
                    records = new[]
                    {
                        new { zoneId = "456", subdomain = "test" }
                    }
                },
                serializerOptions
            );
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"~/config.json", new MockFileData(contents) },
            });

            // When
            var config = await Config.LoadFromAsync("~/config.json", fileSystem);

            // Then
            config.Resolvers.Http.Should().BeEquivalentTo(new HttpResolverConfig
            {
                IPv4Endpoint = "https://custom-ipv4.org",
                IPv6Endpoint = "https://custom-ipv6.org",
            });
        }

        [Fact]
        public async Task Config_LoadFrom_UsesApiTokenFromEnvironmentVariableWhenProvided()
        {
            // Given
            Environment.SetEnvironmentVariable(Config.ENV_CLOUDFLARE_API_TOKEN, "8910");
            var contents = JsonSerializer.Serialize(
                new
                {
                    apiToken = "123",
                    records = new[]
                    {
                        new { zoneId = "456", subdomain = "test" }
                    }
                },
                serializerOptions
            );
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"~/config.json", new MockFileData(contents) },
            });

            // When
            var config = await Config.LoadFromAsync("~/config.json", fileSystem);

            // Then
            Assert.Equal("8910", config.ApiToken);
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        public async Task Config_Loadfrom_ItsPossibleToDisableIPv4OrIPv6Lookups(bool ipv4Enabled, bool ipv6Enabled)
        {
            // Given
            Environment.SetEnvironmentVariable(Config.ENV_CLOUDFLARE_API_TOKEN, "8910");
            var contents = JsonSerializer.Serialize(
                new
                {
                    apiToken = "123",
                    ipv4 = ipv4Enabled,
                    ipv6 = ipv6Enabled,
                    records = new[]
                    {
                        new { zoneId = "456", subdomain = "test" }
                    }
                },
                serializerOptions
            );
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"~/config.json", new MockFileData(contents) },
            });

            // When
            var config = await Config.LoadFromAsync("~/config.json", fileSystem);

            // Then
            Assert.Equal(ipv4Enabled, config.IPv4);
            Assert.Equal(ipv6Enabled, config.IPv6);
        }

        [Fact]
        public async Task Config_LoadFrom_ThrowsAnExceptionWhenApiKeyIsNull()
        {
            // Given
            var contents = JsonSerializer.Serialize(
                new
                {
                    records = new[]
                    {
                        new { zoneId = "456", subdomain = "test" }
                    }
                },
                serializerOptions
            );
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"~/config.json", new MockFileData(contents) },
            });

            // When
            var exception = await Assert.ThrowsAsync<InvalidConfigException>(() => Config.LoadFromAsync("~/config.json", fileSystem));

            // Then
            Assert.Equal("ApiToken", exception.Property);
            Assert.Equal("Property 'ApiToken' can't be null or empty!", exception.Message);
        }

        [Theory]
        [InlineData("123", null, "Subdomain")]
        [InlineData(null, "test", "ZoneId")]
        public async Task Config_LoadFrom_ThrowsAnExceptionWhenARecordsConfigIsInvalid(string zoneId, string subdomain, string property)
        {
            // Given
            var contents = JsonSerializer.Serialize(
                new
                {
                    apiToken = "123",
                    records = new[]
                    {
                        new { zoneId = zoneId, subdomain = subdomain }
                    }
                },
                serializerOptions
            );
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"~/config.json", new MockFileData(contents) },
            });

            // When
            var exception = await Assert.ThrowsAsync<InvalidConfigException>(() => Config.LoadFromAsync("~/config.json", fileSystem));

            // Then
            Assert.Equal(property, exception.Property);
            Assert.Equal($"Property '{property}' can't be null or empty!", exception.Message);
        }

        [Fact]
        public async Task Config_LoadFrom_ThrowsAnExceptionWhenGivenAnUnsupportedDnsServer()
        {
            // Given
            var contents = JsonSerializer.Serialize(
                new
                {
                    apiToken = "123",
                    resolvers = new
                    {
                        dns = "unsupported",
                    },
                    records = new[]
                    {
                        new { zoneId = "456", subdomain = "test" }
                    }
                },
                serializerOptions
            );
            var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"~/config.json", new MockFileData(contents) },
            });

            // When
            var exception = await Assert.ThrowsAsync<InvalidConfigException>(() => Config.LoadFromAsync("~/config.json", fileSystem));

            // Then
            Assert.Equal("DnsServer", exception.Property);
            Assert.Equal($"Property 'DnsServer' can only be 'cloudflare' or 'google'!", exception.Message);
        }
    }
}

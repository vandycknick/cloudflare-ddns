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
                    dns = new[]
                    {
                        new { zoneId = "456", domain = "test" }
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
            Assert.Equal("https://api.ipify.org", config.IPv4Resolver);
            Assert.Equal("https://api6.ipify.org", config.IPv6Resolver);
            Assert.Equal("123", config.ApiToken);
            Assert.Single(config.Dns);
            Assert.Collection(
                config.Dns,
                item => item.Should().BeEquivalentTo(new DnsConfig
                {
                    ZoneId = "456",
                    Domain = "test",
                    Proxied = false,
                })
            );
        }

        [Fact]
        public async Task Config_LoadFrom_ParsesAGivenConfigFileWithCustomIPResolveEndpoints()
        {
            // Given
            var contents = JsonSerializer.Serialize(
                new
                {
                    ipv4Resolver = "https://custom-ipv4.org",
                    ipv6Resolver = "https://custom-ipv6.org",
                    apiToken = "123",
                    dns = new[]
                    {
                        new { zoneId = "456", domain = "test" }
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
            Assert.Equal("https://custom-ipv4.org", config.IPv4Resolver);
            Assert.Equal("https://custom-ipv6.org", config.IPv6Resolver);
        }

        [Fact]
        public async Task Config_LoadFrom_UsesApiTokenFromEnvironmentVariableWhenProvided()
        {
            // 
            Environment.SetEnvironmentVariable(Config.ENV_CLOUDFLARE_API_TOKEN, "8910");
            var contents = JsonSerializer.Serialize(
                new
                {
                    apiToken = "123",
                    dns = new[]
                    {
                        new { zoneId = "456", domain = "test" }
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

        [Fact]
        public async Task Config_LoadFrom_ThrowsAnExceptionWhenApiKeyIsNull()
        {
            // Given
            var contents = JsonSerializer.Serialize(
                new
                {
                    dns = new[]
                    {
                        new { zondId = "456", domain = "test" }
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
        [InlineData("123", null, "Domain")]
        [InlineData(null, "test", "ZoneId")]
        public async Task Config_LoadFrom_ThrowsAnExceptionWhenADnsConfigIsInvalid(string zoneId, string domain, string property)
        {
            // Given
            var contents = JsonSerializer.Serialize(
                new
                {
                    apiToken = "123",
                    dns = new[]
                    {
                        new { zoneId = zoneId, domain = domain }
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
    }
}

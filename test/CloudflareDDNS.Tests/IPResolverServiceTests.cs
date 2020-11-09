using System;
using System.Net;
using System.Threading.Tasks;
using CloudflareDDNS.Service;
using FluentAssertions;
using Moq;
using RichardSzalay.MockHttp;
using Serilog;
using Xunit;

namespace CloudflareDDNS.Tests
{
    public class IPResolverServiceTests
    {
        private readonly MockHttpMessageHandler _mockHttp;
        private readonly Mock<ILogger> _mockLogger;

        public IPResolverServiceTests()
        {
            _mockHttp = new MockHttpMessageHandler();
            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void IPResolverService_Constructor_ThrowsWhenHttpClientIsNull()
        {
            // Given
            var exception = Assert.Throws<ArgumentNullException>(() => new IPResolverService(null, null, null));

            // Then
            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void IPResolverService_Constructor_ThrowsWhenLoggerIsNull()
        {
            // Given, When
            var exception = Assert.Throws<ArgumentNullException>(() => new IPResolverService(_mockHttp.ToHttpClient(), null, null));

            // Then
            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public void IPResolverService_Constructor_ThrowsWhenCloudflareApiOptionsIsNull()
        {
            // Given, When
            var exception = Assert.Throws<ArgumentNullException>(() => new IPResolverService(_mockHttp.ToHttpClient(), _mockLogger.Object, null));

            // Then
            Assert.Equal("options", exception.ParamName);
        }

        [Fact]
        public async Task IPResolverService_Resolve_ReturnsAListOfResolvedIPAdresses()
        {
            // Given
            var ipv4Url = "https://ipv4";
            var ipv6Url = "https://ipv6";
            var options = new IPResolverServiceOptions(ipv4Url, ipv6Url);
            var logger = _mockLogger.Object;

            _mockHttp
                .Expect(ipv4Url)
                .Respond(HttpStatusCode.OK, "text/plain", "10.0.0.1");

            _mockHttp
                .Expect(ipv6Url)
                .Respond(HttpStatusCode.OK, "text/plain", "2001:0db8:85a3:0000:0000:8a2e:0370:7334");

            var client = _mockHttp.ToHttpClient();

            // When
            var ipResolver = new IPResolverService(client, logger, options);
            var ips = await ipResolver.Resolve();

            // Then
            Assert.Equal(2, ips.Count);
            Assert.Collection(
                ips,
                ipv4 => ipv4.Should().BeEquivalentTo(IPAddress.Parse("10.0.0.1")),
                ipv6 => ipv6.Should().BeEquivalentTo(IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"))
            );
        }

        [Fact]
        public async Task IPResolverService_Resolve_OnlyReturnsIPv4AddressWhenRequestingIPv6Fails()
        {
            // Given
            var ipv4Url = "https://ipv4";
            var ipv6Url = "https://ipv6";
            var options = new IPResolverServiceOptions(ipv4Url, ipv6Url);
            var logger = _mockLogger.Object;

            _mockHttp
                .Expect(ipv4Url)
                .Respond(HttpStatusCode.OK, "text/plain", "10.0.0.1");

            var client = _mockHttp.ToHttpClient();

            // When
            var ipResolver = new IPResolverService(client, logger, options);
            var ips = await ipResolver.Resolve();

            // Then
            Assert.Single(ips);
            Assert.Collection(
                ips,
                ipv4 => ipv4.Should().BeEquivalentTo(IPAddress.Parse("10.0.0.1"))
            );
        }

        [Fact]
        public async Task IPResolverService_Resolve_OnlyReturnsIPv6AddressWhenRequestingIPv4Fails()
        {
            // Given
            var ipv4Url = "https://ipv4";
            var ipv6Url = "https://ipv6";
            var options = new IPResolverServiceOptions(ipv4Url, ipv6Url);
            var logger = _mockLogger.Object;

            _mockHttp
                .Expect(ipv6Url)
                .Respond(HttpStatusCode.OK, "text/plain", "2001:0db8:85a3:0000:0000:8a2e:0370:7334");

            var client = _mockHttp.ToHttpClient();

            // When
            var ipResolver = new IPResolverService(client, logger, options);
            var ips = await ipResolver.Resolve();

            // Then
            Assert.Single(ips);
            Assert.Collection(
                ips,
                ipv6 => ipv6.Should().BeEquivalentTo(IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"))
            );
        }


        [Fact]
        public async Task IPResolverService_Resolve_ReturnsAnEmptyListWhenNoAddressesCanBeResolved()
        {
            // Given
            var ipv4Url = "https://ipv4";
            var ipv6Url = "https://ipv6";
            var options = new IPResolverServiceOptions(ipv4Url, ipv6Url);
            var client = _mockHttp.ToHttpClient();
            var logger = _mockLogger.Object;

            // When
            var ipResolver = new IPResolverService(client, logger, options);
            var ips = await ipResolver.Resolve();

            // Then
            Assert.Empty(ips);
        }
    }
}

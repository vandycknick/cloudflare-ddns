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
    public class PublicIPResolverOverHttpTests
    {
        private readonly MockHttpMessageHandler _mockHttp;
        private readonly Mock<ILogger> _mockLogger;

        public PublicIPResolverOverHttpTests()
        {
            _mockHttp = new MockHttpMessageHandler();
            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void PublicIPResolverOverHttp_Constructor_ThrowsWhenHttpClientIsNull()
        {
            // Given
            var exception = Assert.Throws<ArgumentNullException>(() => new PublicIPResolverOverHttp(null, null, null));

            // Then
            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void PublicIPResolverOverHttp_Constructor_ThrowsWhenLoggerIsNull()
        {
            // Given, When
            var exception = Assert.Throws<ArgumentNullException>(() => new PublicIPResolverOverHttp(_mockHttp.ToHttpClient(), null, null));

            // Then
            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public void PublicIPResolverOverHttp_Constructor_ThrowsWhenCloudflareApiOptionsIsNull()
        {
            // Given, When
            var exception = Assert.Throws<ArgumentNullException>(() => new PublicIPResolverOverHttp(_mockHttp.ToHttpClient(), _mockLogger.Object, null));

            // Then
            Assert.Equal("options", exception.ParamName);
        }

        [Fact]
        public async Task PublicIPResolverOverHttp_ResolveIPv4_ReturnsAResolvedIPAdresses()
        {
            // Given
            var ipv4Url = "https://ipv4";
            var ipv6Url = "https://ipv6";
            var options = new PublicIPResolverOverHttpOptions(ipv4Url, ipv6Url);
            var logger = _mockLogger.Object;

            _mockHttp
                .Expect(ipv4Url)
                .Respond(HttpStatusCode.OK, "text/plain", "10.0.0.1");

            // _mockHttp
            //     .Expect(ipv6Url)
            //     .Respond(HttpStatusCode.OK, "text/plain", "2001:0db8:85a3:0000:0000:8a2e:0370:7334");

            var client = _mockHttp.ToHttpClient();

            // When
            var ipResolver = new PublicIPResolverOverHttp(client, logger, options);
            var ipv4 = await ipResolver.ResolveIPv4();

            // Then
            ipv4.Should().BeEquivalentTo(IPAddress.Parse("10.0.0.1"));
        }

        [Fact]
        public async Task PublicIPResolverOverHttp_ResolveIPv6_ReturnsAResolvedIPAdresses()
        {
            // Given
            var ipv4Url = "https://ipv4";
            var ipv6Url = "https://ipv6";
            var options = new PublicIPResolverOverHttpOptions(ipv4Url, ipv6Url);
            var logger = _mockLogger.Object;

            _mockHttp
                .Expect(ipv6Url)
                .Respond(HttpStatusCode.OK, "text/plain", "2001:0db8:85a3:0000:0000:8a2e:0370:7334");

            var client = _mockHttp.ToHttpClient();

            // When
            var ipResolver = new PublicIPResolverOverHttp(client, logger, options);
            var ipv6 = await ipResolver.ResolveIPv6();

            // Then
            ipv6.Should().BeEquivalentTo(IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"));
        }

        [Fact]
        public async Task PublicIPResolverOverHttp_ResolveIPv4_ReturnsNullWhenTheRequestFails()
        {
            // Given
            var ipv4Url = "https://ipv4";
            var ipv6Url = "https://ipv6";
            var options = new PublicIPResolverOverHttpOptions(ipv4Url, ipv6Url);
            var logger = _mockLogger.Object;

            var client = _mockHttp.ToHttpClient();

            // When
            var ipResolver = new PublicIPResolverOverHttp(client, logger, options);
            var ip = await ipResolver.ResolveIPv4();

            // Then
            Assert.Null(ip);
        }

        [Fact]
        public async Task PublicIPResolverOverHttp_ResolveIPv6_ReturnsNullWhenTheRequestFails()
        {
            // Given
            var ipv4Url = "https://ipv4";
            var ipv6Url = "https://ipv6";
            var options = new PublicIPResolverOverHttpOptions(ipv4Url, ipv6Url);
            var logger = _mockLogger.Object;

            var client = _mockHttp.ToHttpClient();

            // When
            var ipResolver = new PublicIPResolverOverHttp(client, logger, options);
            var ip = await ipResolver.ResolveIPv6();

            // Then
            Assert.Null(ip);
        }

        [Fact]
        public async Task PublicIPResolverOverHttp_ResolveIPv4_ReturnsNullWhenAnInvalidIpIsReturned()
        {
            // Given
            var ipv4Url = "https://ipv4";
            var ipv6Url = "https://ipv6";
            var options = new PublicIPResolverOverHttpOptions(ipv4Url, ipv6Url);
            var logger = _mockLogger.Object;

            _mockHttp
                .Expect(ipv4Url)
                .Respond(HttpStatusCode.OK, "text/plain", "abc");

            var client = _mockHttp.ToHttpClient();

            // When
            var ipResolver = new PublicIPResolverOverHttp(client, logger, options);
            var ips = await ipResolver.ResolveIPv4();

            // Then
            Assert.Null(ips);
            _mockLogger.Verify(l => l.Verbose("Can't resolve {type} from {endpoint}: {message}", "ipv4", "https://ipv4", "An invalid IP address was specified."));
        }

        [Fact]
        public async Task PublicIPResolverOverHttp_ResolveIPv6_ReturnsNullWhenAnInvalidIpIsReturned()
        {
            // Given
            var ipv4Url = "https://ipv4";
            var ipv6Url = "https://ipv6";
            var options = new PublicIPResolverOverHttpOptions(ipv4Url, ipv6Url);
            var logger = _mockLogger.Object;

            _mockHttp
                .Expect(ipv6Url)
                .Respond(HttpStatusCode.OK, "text/plain", "");

            var client = _mockHttp.ToHttpClient();

            // When
            var ipResolver = new PublicIPResolverOverHttp(client, logger, options);
            var ips = await ipResolver.ResolveIPv6();

            // Then
            Assert.Null(ips);
            _mockLogger.Verify(l => l.Verbose("Can't resolve {type} from {endpoint}: {message}", "ipv6", "https://ipv6", "An invalid IP address was specified."));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CloudflareDDNS.Service;
using DnsClient;
using DnsClient.Protocol;
using FluentAssertions;
using Moq;
using Serilog;
using Xunit;

namespace CloudflareDDNS.Tests
{
    public class PublicIPResolverOverDnsTests
    {
        private readonly Mock<ILookupClient> _mockLookupClient;
        private readonly Mock<ILogger> _mockLogger;

        public PublicIPResolverOverDnsTests()
        {
            _mockLookupClient = new Mock<ILookupClient>();
            _mockLogger = new Mock<ILogger>();
        }

        [Fact]
        public void PublicIPResolverOverDns_GetOptionsForDnsServer_ReturnTheCorrectOptionsForTheRequestedServer()
        {
            // Given, When
            var cf = PublicIPResolverOverDns.GetOptionsForDnsServer("cloudflare");
            var google = PublicIPResolverOverDns.GetOptionsForDnsServer("google");

            // Then
            Assert.Equal("whoami.cloudflare", cf.Query);
            Assert.Equal("o-o.myaddr.l.google.com", google.Query);
        }

        [Fact]
        public void PublicIPResolverOverDns_GetOptionsForDnsServer_ThrowsAnExceptionForAnUnknownServer()
        {
            // Given, When
            var exception = Assert.Throws<Exception>(() => PublicIPResolverOverDns.GetOptionsForDnsServer("unknown"));

            // Then
            Assert.Equal("Unknown server!", exception.Message);
        }

        [Fact]
        public void PublicIPResolverOverDns_Constructor_ThrowsWhenLookupClientIsNull()
        {
            // Given
            var exception = Assert.Throws<ArgumentNullException>(() => new PublicIPResolverOverDns(null, null, null));

            // Then
            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void PublicIPResolverOverDns_Constructor_ThrowsWhenLoggerIsNull()
        {
            // Given, When
            var exception = Assert.Throws<ArgumentNullException>(() => new PublicIPResolverOverDns(_mockLookupClient.Object, null, null));

            // Then
            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public void PublicIPResolverOverDns_Constructor_ThrowsWhenCloudflareApiOptionsIsNull()
        {
            // Given, When
            var exception = Assert.Throws<ArgumentNullException>(() => new PublicIPResolverOverDns(_mockLookupClient.Object, _mockLogger.Object, null));

            // Then
            Assert.Equal("options", exception.ParamName);
        }

        [Fact]
        public async Task PublicIPResolverOverDns_ResolveIPv4_ReturnsAnIPv4Address()
        {
            // Given
            var mockResponse = new Mock<IDnsQueryResponse>();
            var addresses = new List<IPAddress> { IPAddress.Parse("1.1.1.1") };
            var options = new PublicIPResolverOverDnsOptions("myip.test.com", QueryType.TXT, QueryClass.CH, addresses, addresses);

            mockResponse
                .SetupGet(r => r.Answers)
                .Returns(new List<DnsResourceRecord>
                {
                    new TxtRecord(
                        info: new ResourceRecordInfo("myip.test.com", ResourceRecordType.TXT, QueryClass.CH, 5000, 0),
                        values: new string[] { },
                        utf8Values: new string[] { "10.0.0.1" }
                    )
                });

            _mockLookupClient
                .Setup(dns => dns.QueryAsync(It.IsAny<DnsQuestion>(), It.IsAny<DnsQueryAndServerOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // When
            var resolver = new PublicIPResolverOverDns(_mockLookupClient.Object, _mockLogger.Object, options);
            var ip = await resolver.ResolveIPv4();

            // Then
            ip.Should().BeEquivalentTo(IPAddress.Parse("10.0.0.1"));
        }

        [Fact]
        public async Task PublicIPResolverOverDns_ResolveIPv4_ReturnsTheFirstStringAsAnIPv4AddressWhenMultipleAreReturned()
        {
            // Given
            var mockResponse = new Mock<IDnsQueryResponse>();
            var addresses = new List<IPAddress> { IPAddress.Parse("1.1.1.1") };
            var options = new PublicIPResolverOverDnsOptions("myip.test.com", QueryType.TXT, QueryClass.CH, addresses, addresses);

            mockResponse
                .SetupGet(r => r.Answers)
                .Returns(new List<DnsResourceRecord>
                {
                    new TxtRecord(
                        info: new ResourceRecordInfo("myip.test.com", ResourceRecordType.TXT, QueryClass.CH, 5000, 0),
                        values: new string[] { },
                        utf8Values: new string[] { "10.4.4.1", "10.0.0.1" }
                    )
                });

            _mockLookupClient
                .Setup(dns => dns.QueryAsync(It.IsAny<DnsQuestion>(), It.IsAny<DnsQueryAndServerOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // When
            var resolver = new PublicIPResolverOverDns(_mockLookupClient.Object, _mockLogger.Object, options);
            var ip = await resolver.ResolveIPv4();

            // Then
            ip.Should().BeEquivalentTo(IPAddress.Parse("10.4.4.1"));
        }

        [Fact]
        public async Task PublicIPResolverOverDns_ResolveIPv4_ReturnsNullWhenNoTxtRecordsAreReturned()
        {
            // Given
            var mockResponse = new Mock<IDnsQueryResponse>();
            var addresses = new List<IPAddress> { IPAddress.Parse("1.1.1.1") };
            var options = new PublicIPResolverOverDnsOptions("myip.test.com", QueryType.TXT, QueryClass.CH, addresses, addresses);

            mockResponse
                .SetupGet(r => r.Answers)
                .Returns(new List<DnsResourceRecord>
                {
                });

            _mockLookupClient
                .Setup(dns => dns.QueryAsync(It.IsAny<DnsQuestion>(), It.IsAny<DnsQueryAndServerOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // When
            var resolver = new PublicIPResolverOverDns(_mockLookupClient.Object, _mockLogger.Object, options);
            var ip = await resolver.ResolveIPv4();

            // Then
            Assert.Null(ip);
        }


        [Fact]
        public async Task PublicIPResolverOverDns_ResolveIPv4_ReturnsNullWhenAnInvalidIpIsReturned()
        {
            // Given
            var mockResponse = new Mock<IDnsQueryResponse>();
            var addresses = new List<IPAddress> { IPAddress.Parse("1.1.1.1") };
            var options = new PublicIPResolverOverDnsOptions("myip.test.com", QueryType.TXT, QueryClass.IN, addresses, addresses);

            mockResponse
                .SetupGet(r => r.Answers)
                .Returns(new List<DnsResourceRecord>
                {
                    new TxtRecord(
                        info: new ResourceRecordInfo("myip.test.com", ResourceRecordType.TXT, QueryClass.IN, 5000, 0),
                        values: new string[] { },
                        utf8Values: new string[] { "abc" }
                    )
                });

            _mockLookupClient
                .Setup(dns => dns.QueryAsync(It.IsAny<DnsQuestion>(), It.IsAny<DnsQueryAndServerOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // When
            var resolver = new PublicIPResolverOverDns(_mockLookupClient.Object, _mockLogger.Object, options);
            var ip = await resolver.ResolveIPv4();

            // Then
            Assert.Null(ip);
        }

        [Fact]
        public async Task PublicIPResolverOverDns_ResolveIPv6_ReturnsAnIPv6Address()
        {
            // Given
            var mockResponse = new Mock<IDnsQueryResponse>();
            var addresses = new List<IPAddress> { IPAddress.Parse("2606:4700:4700::1111") };
            var options = new PublicIPResolverOverDnsOptions("myip.test.com", QueryType.TXT, QueryClass.IN, addresses, addresses);

            mockResponse
                .SetupGet(r => r.Answers)
                .Returns(new List<DnsResourceRecord>
                {
                    new TxtRecord(
                        info: new ResourceRecordInfo("myip.test.com", ResourceRecordType.TXT, QueryClass.IN, 5000, 0),
                        values: new string[] { },
                        utf8Values: new string[] { "2001:0db8:85a3:0000:0000:8a2e:0370:7334" }
                    )
                });

            _mockLookupClient
                .Setup(dns => dns.QueryAsync(It.IsAny<DnsQuestion>(), It.IsAny<DnsQueryAndServerOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // When
            var resolver = new PublicIPResolverOverDns(_mockLookupClient.Object, _mockLogger.Object, options);
            var ip = await resolver.ResolveIPv6();

            // Then
            ip.Should().BeEquivalentTo(IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"));
        }

        [Fact]
        public async Task PublicIPResolverOverDns_ResolveIPv6_ReturnsNullWhenDnsLookupThrowsAnException()
        {
            // Given
            var mockResponse = new Mock<IDnsQueryResponse>();
            var addresses = new List<IPAddress> { IPAddress.Parse("2606:4700:4700::1111") };
            var options = new PublicIPResolverOverDnsOptions("myip.test.com", QueryType.TXT, QueryClass.IN, addresses, addresses);

            mockResponse
                .SetupGet(r => r.Answers)
                .Throws(new Exception("Some exception"));

            _mockLookupClient
                .Setup(dns => dns.QueryAsync(It.IsAny<DnsQuestion>(), It.IsAny<DnsQueryAndServerOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // When
            var resolver = new PublicIPResolverOverDns(_mockLookupClient.Object, _mockLogger.Object, options);
            var ip = await resolver.ResolveIPv6();

            // Then
            _mockLogger.Verify(l => l.Verbose("Error resolving {ip} over dns: {message}", "ipv6", "Some exception"));
            Assert.Null(ip);
        }
    }
}

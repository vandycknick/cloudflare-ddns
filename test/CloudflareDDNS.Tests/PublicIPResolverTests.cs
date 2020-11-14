using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;

namespace CloudflareDDNS.Service
{
    public class PublicIPResolverTests
    {
        private readonly Mock<IPublicIPResolver> _mockNullResolver;
        private readonly Mock<IPublicIPResolver> _mockResolver;
        private readonly Mock<IPublicIPResolver> _mockResolver2;
        public PublicIPResolverTests()
        {
            _mockNullResolver = new Mock<IPublicIPResolver>();
            _mockResolver = new Mock<IPublicIPResolver>();
            _mockResolver2 = new Mock<IPublicIPResolver>();

            _mockNullResolver
                .Setup(n => n.ResolveIPv4()).ReturnsAsync((IPAddress)null);
            _mockNullResolver
                .Setup(n => n.ResolveIPv6()).ReturnsAsync((IPAddress)null);

            _mockResolver
                .Setup(n => n.ResolveIPv4()).ReturnsAsync(IPAddress.Parse("10.0.0.1"));
            _mockResolver
                .Setup(n => n.ResolveIPv6()).ReturnsAsync(IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"));

            _mockResolver2
                .Setup(n => n.ResolveIPv4()).ReturnsAsync(IPAddress.Parse("10.0.0.2"));
            _mockResolver2
                .Setup(n => n.ResolveIPv6()).ReturnsAsync(IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:5555"));
        }

        [Fact]
        public void PublicIPResolver_Constructor_ThrowsAnExceptionWhenResolversIsNull()
        {
            // Given, When
            var exception = Assert.Throws<ArgumentNullException>(() => new PublicIPResolver(null));

            // Then
            Assert.Equal("resolvers", exception.ParamName);
        }

        [Fact]
        public async Task PublicIPResolver_ResolveIPv4_ReturnsTheFirstNonNullIP()
        {
            // Given
            var resolver = new PublicIPResolver(_mockNullResolver.Object, _mockNullResolver.Object, _mockResolver2.Object, _mockResolver.Object);

            // When
            var ip = await resolver.ResolveIPv4();

            // Then
            ip.Should().BeEquivalentTo(IPAddress.Parse("10.0.0.2"));
        }

        [Fact]
        public async Task PublicIPResolver_ResolveIPv4_ReturnsNullWhenNoResolverReturnsAnIP()
        {
            // Given
            var resolver = new PublicIPResolver(_mockNullResolver.Object, _mockNullResolver.Object);

            // When
            var ip = await resolver.ResolveIPv4();

            // 
            Assert.Null(ip);
        }

        [Fact]
        public async Task PublicIPResolver_ResolveIPv6_ReturnsTheFirstNonNullIP()
        {
            // Given
            var resolver = new PublicIPResolver(_mockNullResolver.Object, _mockNullResolver.Object, _mockResolver2.Object, _mockResolver.Object);

            // When
            var ip = await resolver.ResolveIPv6();

            // Then
            ip.Should().BeEquivalentTo(IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:5555"));
        }

        [Fact]
        public async Task PublicIPResolver_ResolveIPv6_ReturnsNullWhenNoResolverReturnsAnIP()
        {
            // Given
            var resolver = new PublicIPResolver(_mockNullResolver.Object, _mockNullResolver.Object);

            // When
            var ip = await resolver.ResolveIPv6();

            // 
            Assert.Null(ip);
        }
    }
}

using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CloudflareDDNS.Service;
using Moq;
using Serilog;
using Xunit;

namespace CloudflareDDNS.Tests
{
    public class ProgramTests
    {
        private readonly Mock<IPublicIPResolver> _mockNullResolver;
        private readonly Mock<IPublicIPResolver> _mockResolver;
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IDynamicDnsService> _mockDynamicDnsService;

        public ProgramTests()
        {
            _mockNullResolver = new Mock<IPublicIPResolver>();
            _mockResolver = new Mock<IPublicIPResolver>();
            _mockLogger = new Mock<ILogger>();
            _mockDynamicDnsService = new Mock<IDynamicDnsService>();

            _mockNullResolver
                .Setup(n => n.ResolveIPv4()).ReturnsAsync((IPAddress)null);
            _mockNullResolver
                .Setup(n => n.ResolveIPv6()).ReturnsAsync((IPAddress)null);

            _mockResolver
                .Setup(n => n.ResolveIPv4()).ReturnsAsync(IPAddress.Parse("10.0.0.1"));
            _mockResolver
                .Setup(n => n.ResolveIPv6()).ReturnsAsync(IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"));

        }

        [Fact]
        public async Task Program_SyncDNSCommand_UpsertsADnsRecordForAIPv4AndIPv6Record()
        {
            // Given
            var config = new Config
            {
                Records = new List<RecordsConfig>
                {
                    new RecordsConfig
                    {
                        ZoneId = "123",
                        Domain = "yolo",
                        Proxied = true
                    }
                }
            };

            // When
            await Program.SyncDNSCommand(
                _mockLogger.Object,
                _mockResolver.Object,
                _mockDynamicDnsService.Object,
                config
            );

            // Then
            _mockDynamicDnsService
                .Verify(ddns => ddns.UpsertRecords("123", "yolo", true, new List<IPAddress>
                {
                    IPAddress.Parse("10.0.0.1"),
                    IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"),
                }));
            _mockDynamicDnsService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Program_SyncDNSCommand_UpsertsADnsRecordForAIPv4AndIPv6RecordForEachConfiguredDomain()
        {
            // Given
            var config = new Config
            {
                Records = new List<RecordsConfig>
                {
                    new RecordsConfig
                    {
                        ZoneId = "123",
                        Domain = "yolo",
                        Proxied = true
                    },
                    new RecordsConfig
                    {
                        ZoneId = "123",
                        Domain = "yolo2",
                        Proxied = true
                    }
                }
            };

            // When
            await Program.SyncDNSCommand(
                _mockLogger.Object,
                _mockResolver.Object,
                _mockDynamicDnsService.Object,
                config
            );

            // Then
            _mockDynamicDnsService
                .Verify(ddns => ddns.UpsertRecords("123", "yolo", true, new List<IPAddress>
                {
                    IPAddress.Parse("10.0.0.1"),
                    IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"),
                }));
            _mockDynamicDnsService
                .Verify(ddns => ddns.UpsertRecords("123", "yolo2", true, new List<IPAddress>
                {
                    IPAddress.Parse("10.0.0.1"),
                    IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"),
                }));
            _mockDynamicDnsService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Program_SyncDNSCommand_UpsertsADnsRecordForAIPv4RecordWhenItCantFindIPv6Address()
        {
            // Given
            var config = new Config
            {
                Records = new List<RecordsConfig>
                {
                    new RecordsConfig
                    {
                        ZoneId = "123",
                        Domain = "yolo",
                        Proxied = true
                    }
                }
            };

            _mockResolver.Reset();
            _mockResolver
                .Setup(n => n.ResolveIPv4()).ReturnsAsync(IPAddress.Parse("10.0.0.1"));

            // When
            await Program.SyncDNSCommand(
                _mockLogger.Object,
                _mockResolver.Object,
                _mockDynamicDnsService.Object,
                config
            );

            // Then
            _mockDynamicDnsService
                .Verify(ddns => ddns.UpsertRecords("123", "yolo", true, new List<IPAddress>
                {
                    IPAddress.Parse("10.0.0.1"),
                }));
            _mockDynamicDnsService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Program_SyncDNSCommand_UpsertsADnsRecordForAIPv6RecordWhenItCantFindIPv4Address()
        {
            // Given
            var config = new Config
            {
                Records = new List<RecordsConfig>
                {
                    new RecordsConfig
                    {
                        ZoneId = "123",
                        Domain = "yolo",
                        Proxied = true
                    }
                }
            };

            _mockResolver.Reset();
            _mockResolver
                .Setup(n => n.ResolveIPv6()).ReturnsAsync(IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"));

            // When
            await Program.SyncDNSCommand(
                _mockLogger.Object,
                _mockResolver.Object,
                _mockDynamicDnsService.Object,
                config
            );

            // Then
            _mockDynamicDnsService
                .Verify(ddns => ddns.UpsertRecords("123", "yolo", true, new List<IPAddress>
                {
                    IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"),
                }));
            _mockDynamicDnsService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Program_SyncDNSCommand_UpsertsADnsRecordForAIPv6WhenIPv4IsDisabled()
        {
            // Given
            var config = new Config
            {
                IPv4 = false,
                Records = new List<RecordsConfig>
                {
                    new RecordsConfig
                    {
                        ZoneId = "123",
                        Domain = "yolo",
                        Proxied = true
                    }
                }
            };

            // When
            await Program.SyncDNSCommand(
                _mockLogger.Object,
                _mockResolver.Object,
                _mockDynamicDnsService.Object,
                config
            );

            // Then
            _mockDynamicDnsService
                .Verify(ddns => ddns.UpsertRecords("123", "yolo", true, new List<IPAddress>
                {
                    IPAddress.Parse("2001:0db8:85a3:0000:0000:8a2e:0370:7334"),
                }));
            _mockDynamicDnsService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Program_SyncDNSCommand_UpsertsADnsRecordForAIPv4WhenIPv6IsDisabled()
        {
            // Given
            var config = new Config
            {
                IPv6 = false,
                Records = new List<RecordsConfig>
                {
                    new RecordsConfig
                    {
                        ZoneId = "123",
                        Domain = "yolo",
                        Proxied = true
                    }
                }
            };

            // When
            await Program.SyncDNSCommand(
                _mockLogger.Object,
                _mockResolver.Object,
                _mockDynamicDnsService.Object,
                config
            );

            // Then
            _mockDynamicDnsService
                .Verify(ddns => ddns.UpsertRecords("123", "yolo", true, new List<IPAddress>
                {
                    IPAddress.Parse("10.0.0.1"),
                }));
            _mockDynamicDnsService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Program_SyncDnscommand_LogsAMessagesAndExitsEarlyWhenNoIPsAreResolved()
        {
            // Given
            var config = new Config
            {
                Records = new List<RecordsConfig>
                {
                    new RecordsConfig
                    {
                        ZoneId = "123",
                        Domain = "yolo",
                        Proxied = true
                    }
                }
            };

            // When
            await Program.SyncDNSCommand(
                _mockLogger.Object,
                _mockNullResolver.Object,
                _mockDynamicDnsService.Object,
                config
            );

            // Then
            _mockLogger.Verify(l => l.Warning("Couldn't resolve your public IPv4 or IPv6 address, check your configuration and connectivity. Skipping ddns sync!"));
            _mockDynamicDnsService.VerifyNoOtherCalls();
        }
    }
}

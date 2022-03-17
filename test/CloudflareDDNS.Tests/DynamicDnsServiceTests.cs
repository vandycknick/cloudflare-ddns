using CloudflareDDNS.Api;
using CloudflareDDNS.Api.Models;
using CloudflareDDNS.Service;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace CloudflareDDNS.Tests
{
    public class DynamicDnsServiceTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<ICloudflareApi> _mockCloudflareApi;

        public DynamicDnsServiceTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockCloudflareApi = new Mock<ICloudflareApi>();
        }

        [Fact]
        public void DynamicDnsService_Constructor_ThrowsWhenLoggerIsNull()
        {
            // Given, When
            var exception = Assert.Throws<ArgumentNullException>(() => new DynamicDnsService(null, _mockCloudflareApi.Object));

            // Then
            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public void DynamicDnsService_Constructor_ThrowsWhenCloudflareApiIsNull()
        {
            // Given, When
            var exception = Assert.Throws<ArgumentNullException>(() => new DynamicDnsService(_mockLogger.Object, null));

            // Then
            Assert.Equal("cloudflare", exception.ParamName);
        }

        [Fact]
        public async Task DynamicDnsService_UpsertRecords_CreatesANewRecordForEachIPIfOneDoesNotExistAlready()
        {
            // Given
            var zoneId = "123";
            var subdomain = "domain";
            var domain = "example.com";
            var proxied = true;
            var addresses = new List<IPAddress>
            {
                IPAddress.Parse("10.0.0.1"),
                IPAddress.Parse("2606:4700:4700::1111"),
            };

            _mockCloudflareApi
                .Setup(api => api.GetZoneDetails(zoneId))
                .ReturnsAsync(new Zone
                {
                    Id = zoneId,
                    Name = domain,
                });

            _mockCloudflareApi
                .Setup(api => api.ListDNSRecords(zoneId, null, "domain.example.com", 1, 100))
                .ReturnsAsync(new PagedDnsResult(new List<DnsResult>(), new ApiResultPager()));

            _mockCloudflareApi
                .Setup(api =>
                    api.CreateDNSRecord(zoneId, "A", "domain.example.com", "10.0.0.1", 1, true))
                .ReturnsAsync(new DnsResult
                {
                    Id = "989c23e6-5481-4858-bed1-584bb922579a",
                    Name = subdomain
                });

            _mockCloudflareApi
                .Setup(api =>
                    api.CreateDNSRecord(zoneId, "AAAA", "domain.example.com", "2606:4700:4700::1111", 1, true))
                .ReturnsAsync(new DnsResult
                {
                    Id = "8257dd15-2039-4434-887a-aff5d93af2a1",
                    Name = subdomain
                });

            // When
            var ddnsService = new DynamicDnsService(_mockLogger.Object, _mockCloudflareApi.Object);
            await ddnsService.UpsertRecords(zoneId, subdomain, proxied, addresses);

            // Then
            _mockCloudflareApi.Verify(api =>
                api.CreateDNSRecord(zoneId, "A", "domain.example.com", "10.0.0.1", 1, true));
            _mockCloudflareApi.Verify(api =>
                api.CreateDNSRecord(zoneId, "AAAA", "domain.example.com", "2606:4700:4700::1111", 1, true));
        }

        [Fact]
        public async Task DynamicDnsService_UpsertRecords_CreatesATXTRecordWithChecksumeForEachNewIP()
        {
            // Given
            var zoneId = "123";
            var subdomain = "domain";
            var domain = "example.com";
            var proxied = true;
            var addresses = new List<IPAddress>
            {
                IPAddress.Parse("10.0.0.1"),
                IPAddress.Parse("2606:4700:4700::1111"),
            };

            _mockCloudflareApi
                .Setup(api => api.GetZoneDetails(zoneId))
                .ReturnsAsync(new Zone
                {
                    Id = zoneId,
                    Name = domain,
                });

            _mockCloudflareApi
                .Setup(api => api.ListDNSRecords(zoneId, null, "domain.example.com", 1, 100))
                .ReturnsAsync(new PagedDnsResult(new List<DnsResult>(), new ApiResultPager()));

            _mockCloudflareApi
                .Setup(api =>
                    api.CreateDNSRecord(zoneId, "A", "domain.example.com", "10.0.0.1", 1, true))
                .ReturnsAsync(new DnsResult
                {
                    Id = "989c23e6-5481-4858-bed1-584bb922579a",
                    Name = subdomain
                });

            _mockCloudflareApi
                .Setup(api =>
                    api.CreateDNSRecord(zoneId, "AAAA", "domain.example.com", "2606:4700:4700::1111", 1, true))
                .ReturnsAsync(new DnsResult
                {
                    Id = "8257dd15-2039-4434-887a-aff5d93af2a1",
                    Name = subdomain
                });

            // When
            var ddnsService = new DynamicDnsService(_mockLogger.Object, _mockCloudflareApi.Object);
            await ddnsService.UpsertRecords(zoneId, subdomain, proxied, addresses);

            // Then
            var aContent = "managed-by: cloudflare-ddns, check: OTg5YzIzZTYtNTQ4MS00ODU4LWJlZDEtNTg0YmI5MjI1Nzlh";
            _mockCloudflareApi.Verify(api =>
                api.CreateDNSRecord(zoneId, "TXT", "domain.example.com", aContent, 1, null));
            var aaaaContent = "managed-by: cloudflare-ddns, check: ODI1N2RkMTUtMjAzOS00NDM0LTg4N2EtYWZmNWQ5M2FmMmEx";
            _mockCloudflareApi.Verify(api =>
                api.CreateDNSRecord(zoneId, "TXT", "domain.example.com", aaaaContent, 1, null));
        }

        [Fact]
        public async Task DynamicDnsService_UpsertRecords_UpdatesTheRecordForEachIPIfTheCheckMatches()
        {
            // Given
            var zoneId = "123";
            var subdomain = "domain";
            var domain = "example.com";
            var proxied = true;
            var addresses = new List<IPAddress>
            {
                IPAddress.Parse("10.0.0.1"),
            };

            var records = new List<DnsResult>
            {
                new DnsResult
                {
                    Id = "989c23e6-5481-4858-bed1-584bb922579a",
                    Type = "A",
                    Name = $"{subdomain}.{domain}",
                    Content = "10.0.0.2",
                },
                new DnsResult
                {
                    Id = "2de1e83b-38fa-4e47-a6cc-9c3520bf4a85",
                    Type = "TXT",
                    Name = $"{subdomain}.{domain}",
                    Content = "managed-by: cloudflare-ddns, check: OTg5YzIzZTYtNTQ4MS00ODU4LWJlZDEtNTg0YmI5MjI1Nzlh",
                }
            };

            _mockCloudflareApi
                .Setup(api => api.GetZoneDetails(zoneId))
                .ReturnsAsync(new Zone
                {
                    Id = zoneId,
                    Name = domain,
                });

            _mockCloudflareApi
                .Setup(api => api.ListDNSRecords(zoneId, null, "domain.example.com", 1, 100))
                .ReturnsAsync(new PagedDnsResult(records, new ApiResultPager()));

            // When
            var ddnsService = new DynamicDnsService(_mockLogger.Object, _mockCloudflareApi.Object);
            await ddnsService.UpsertRecords(zoneId, subdomain, proxied, addresses);

            // Then
            _mockCloudflareApi.Verify(api =>
                api.UpdateDNSRecord(zoneId, "989c23e6-5481-4858-bed1-584bb922579a", "A", "domain.example.com", "10.0.0.1", 1, proxied));
        }

        [Fact]
        public async Task DynamicDnsService_UpsertRecords_DoesNotUpdateWhenNoValidCheckTxtRecordIsPresent()
        {
            // Given
            var zoneId = "123";
            var subdomain = "domain";
            var domain = "example.com";
            var proxied = true;
            var addresses = new List<IPAddress>
            {
                IPAddress.Parse("10.0.0.1"),
            };

            var records = new List<DnsResult>
            {
                new DnsResult
                {
                    Id = "989c23e6-5481-4858-bed1-584bb922579a",
                    Type = "A",
                    Name = $"{subdomain}.{domain}",
                    Content = "10.0.0.2",
                },
                new DnsResult
                {
                    Id = "2de1e83b-38fa-4e47-a6cc-9c3520bf4a85",
                    Type = "TXT",
                    Name = $"{subdomain}.{domain}",
                    Content = "managed-by: cloudflare-ddns, check: INVALIDCHECK",
                }
            };

            _mockCloudflareApi
                .Setup(api => api.GetZoneDetails(zoneId))
                .ReturnsAsync(new Zone
                {
                    Id = zoneId,
                    Name = domain,
                });

            _mockCloudflareApi
                .Setup(api => api.ListDNSRecords(zoneId, null, "domain.example.com", 1, 100))
                .ReturnsAsync(new PagedDnsResult(records, new ApiResultPager()));

            // When
            var ddnsService = new DynamicDnsService(_mockLogger.Object, _mockCloudflareApi.Object);
            await ddnsService.UpsertRecords(zoneId, subdomain, proxied, addresses);

            // Then
            _mockCloudflareApi.Verify(api => api.GetZoneDetails(zoneId));
            _mockCloudflareApi.Verify(api => api.ListDNSRecords(zoneId, null, "domain.example.com", 1, 100));
            _mockCloudflareApi.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DynamicDnsService_UpsertRecords_DoesNotUpdateWhenNoCheckTxtRecordIsPresent()
        {
            // Given
            var zoneId = "123";
            var subdomain = "domain";
            var domain = "example.com";
            var proxied = true;
            var addresses = new List<IPAddress>
            {
                IPAddress.Parse("10.0.0.1"),
            };

            var records = new List<DnsResult>
            {
                new DnsResult
                {
                    Id = "989c23e6-5481-4858-bed1-584bb922579a",
                    Type = "A",
                    Name = $"{subdomain}.{domain}",
                    Content = "10.0.0.2",
                }
            };

            _mockCloudflareApi
                .Setup(api => api.GetZoneDetails(zoneId))
                .ReturnsAsync(new Zone
                {
                    Id = zoneId,
                    Name = domain,
                });

            _mockCloudflareApi
                .Setup(api => api.ListDNSRecords(zoneId, null, "domain.example.com", 1, 100))
                .ReturnsAsync(new PagedDnsResult(records, new ApiResultPager()));

            // When
            var ddnsService = new DynamicDnsService(_mockLogger.Object, _mockCloudflareApi.Object);
            await ddnsService.UpsertRecords(zoneId, subdomain, proxied, addresses);

            // Then
            _mockCloudflareApi.Verify(api => api.GetZoneDetails(zoneId));
            _mockCloudflareApi.Verify(api => api.ListDNSRecords(zoneId, null, "domain.example.com", 1, 100));
            _mockCloudflareApi.VerifyNoOtherCalls();
        }


        [Fact]
        public async Task DynamicDnsService_UpsertRecords_LogsAMessageWhenARecordIsUpdated()
        {
            // Given
            var zoneId = "123";
            var subdomain = "domain";
            var domain = "example.com";
            var proxied = true;
            var addresses = new List<IPAddress>
            {
                IPAddress.Parse("10.0.0.1"),
            };

            var records = new List<DnsResult>
            {
                new DnsResult
                {
                    Id = "989c23e6-5481-4858-bed1-584bb922579a",
                    Type = "A",
                    Name = $"{subdomain}.{domain}",
                    Content = "10.0.0.2",
                },
                new DnsResult
                {
                    Id = "2de1e83b-38fa-4e47-a6cc-9c3520bf4a85",
                    Type = "TXT",
                    Name = $"{subdomain}.{domain}",
                    Content = "managed-by: cloudflare-ddns, check: OTg5YzIzZTYtNTQ4MS00ODU4LWJlZDEtNTg0YmI5MjI1Nzlh",
                }
            };

            _mockCloudflareApi
                .Setup(api => api.GetZoneDetails(zoneId))
                .ReturnsAsync(new Zone
                {
                    Id = zoneId,
                    Name = domain,
                });

            _mockCloudflareApi
                .Setup(api => api.ListDNSRecords(zoneId, null, "domain.example.com", 1, 100))
                .ReturnsAsync(new PagedDnsResult(records, new ApiResultPager()));

            // When
            var ddnsService = new DynamicDnsService(_mockLogger.Object, _mockCloudflareApi.Object);
            await ddnsService.UpsertRecords(zoneId, subdomain, proxied, addresses);

            // Then
            _mockLogger.Verify(l => l.Information("Updating existing {type} record for {domain} from {previous} to {current}.", "A", "domain.example.com", "10.0.0.2", "10.0.0.1"));
            _mockLogger.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DynamicDnsService_UpsertRecords_OnlyUpdatesWhenTheIPAddressIsDifferent()
        {
            // Given
            var zoneId = "123";
            var subdomain = "domain";
            var domain = "example.com";
            var proxied = true;
            var addresses = new List<IPAddress>
            {
                IPAddress.Parse("10.0.0.2"),
            };

            var records = new List<DnsResult>
            {
                new DnsResult
                {
                    Id = "989c23e6-5481-4858-bed1-584bb922579a",
                    Type = "A",
                    Name = $"{subdomain}.{domain}",
                    Content = "10.0.0.2",
                },
                new DnsResult
                {
                    Id = "2de1e83b-38fa-4e47-a6cc-9c3520bf4a85",
                    Type = "TXT",
                    Name = $"{subdomain}.{domain}",
                    Content = "managed-by: cloudflare-ddns, check: OTg5YzIzZTYtNTQ4MS00ODU4LWJlZDEtNTg0YmI5MjI1Nzlh",
                }
            };

            _mockCloudflareApi
                .Setup(api => api.GetZoneDetails(zoneId))
                .ReturnsAsync(new Zone
                {
                    Id = zoneId,
                    Name = domain,
                });

            _mockCloudflareApi
                .Setup(api => api.ListDNSRecords(zoneId, null, "domain.example.com", 1, 100))
                .ReturnsAsync(new PagedDnsResult(records, new ApiResultPager()));

            // When
            var ddnsService = new DynamicDnsService(_mockLogger.Object, _mockCloudflareApi.Object);
            await ddnsService.UpsertRecords(zoneId, subdomain, proxied, addresses);

            // Then
            _mockCloudflareApi.Verify(api => api.GetZoneDetails(zoneId));
            _mockCloudflareApi.Verify(api => api.ListDNSRecords(zoneId, null, "domain.example.com", 1, 100));
            _mockCloudflareApi.VerifyNoOtherCalls();
            _mockLogger.Verify(l => l.Information("Record type {type} for {domain} is already up to date!", "A", "domain.example.com"));
            _mockLogger.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task DynamicDnsService_UpsertRecords_LogsAMessageWhenARecordAlreadyExitsAndIsNotManagedByCloudflareDDNS()
        {
            // Given
            var zoneId = "123";
            var subdomain = "domain";
            var domain = "example.com";
            var proxied = true;
            var addresses = new List<IPAddress>
            {
                IPAddress.Parse("10.0.0.1"),
            };

            var records = new List<DnsResult>
            {
                new DnsResult
                {
                    Id = "989c23e6-5481-4858-bed1-584bb922579a",
                    Type = "A",
                    Name = $"{subdomain}.{domain}",
                    Content = "10.0.0.2",
                }
            };

            _mockCloudflareApi
                .Setup(api => api.GetZoneDetails(zoneId))
                .ReturnsAsync(new Zone
                {
                    Id = zoneId,
                    Name = domain,
                });

            _mockCloudflareApi
                .Setup(api => api.ListDNSRecords(zoneId, null, "domain.example.com", 1, 100))
                .ReturnsAsync(new PagedDnsResult(records, new ApiResultPager()));

            // When
            var ddnsService = new DynamicDnsService(_mockLogger.Object, _mockCloudflareApi.Object);
            await ddnsService.UpsertRecords(zoneId, subdomain, proxied, addresses);

            // Then
            _mockLogger.Verify(l => l.Information("An {type} record with {domain} already exists that's currently not managed by cloudflare-ddns.", "A", domain));
        }

        [Fact]
        public async Task DynamicDnsService_UpsertRecords_BailsOutAndLogsAMessageWhenACNAMERecordAlreadyExits()
        {
            // Given
            var zoneId = "123";
            var subdomain = "domain";
            var domain = "example.com";
            var proxied = true;
            var addresses = new List<IPAddress>
            {
                IPAddress.Parse("10.0.0.1"),
            };

            var records = new List<DnsResult>
            {
                new DnsResult
                {
                    Id = "989c23e6-5481-4858-bed1-584bb922579a",
                    Type = "CNAME",
                    Name = $"{subdomain}.{domain}",
                    Content = "10.0.0.2",
                }
            };

            _mockCloudflareApi
                .Setup(api => api.GetZoneDetails(zoneId))
                .ReturnsAsync(new Zone
                {
                    Id = zoneId,
                    Name = domain,
                });

            _mockCloudflareApi
                .Setup(api => api.ListDNSRecords(zoneId, null, "domain.example.com", 1, 100))
                .ReturnsAsync(new PagedDnsResult(records, new ApiResultPager()));

            // When
            var ddnsService = new DynamicDnsService(_mockLogger.Object, _mockCloudflareApi.Object);
            await ddnsService.UpsertRecords(zoneId, subdomain, proxied, addresses);

            // Then
            _mockCloudflareApi.Verify(api => api.GetZoneDetails(zoneId));
            _mockCloudflareApi.Verify(api => api.ListDNSRecords(zoneId, null, "domain.example.com", 1, 100));
            _mockCloudflareApi.VerifyNoOtherCalls();

            _mockLogger.Verify(l => l.Error("A CNAME record already exists for {domain}, please remove if you intended for cloudflare-ddns to manage this record!", $"{subdomain}.{domain}"));
            _mockLogger.VerifyNoOtherCalls();
        }

    }
}

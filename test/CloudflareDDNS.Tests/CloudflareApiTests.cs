using CloudflareDDNS.Api;
using CloudflareDDNS.Api.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using RichardSzalay.MockHttp;
using Xunit;
using System.Net.Http;

namespace CloudflareDDNS.Tests
{
    public class CloudflareApiTests
    {
        private readonly MockHttpMessageHandler _mockHttp;

        public CloudflareApiTests()
        {
            _mockHttp = new MockHttpMessageHandler();
        }

        [Fact]
        public void CloudflareApi_Constructor_ThrowsWhenHttpClientIsNull()
        {
            // Given
            var exception = Assert.Throws<ArgumentNullException>(() => new CloudflareApi(null, null));

            // Then
            Assert.Equal("client", exception.ParamName);
        }

        [Fact]
        public void CloudflareApi_Constructor_ThrowsWhenCloudflareApiOptionsIsNull()
        {
            // Given, When
            var exception = Assert.Throws<ArgumentNullException>(() => new CloudflareApi(_mockHttp.ToHttpClient(), null));

            // Then
            Assert.Equal("options", exception.ParamName);
        }

        [Fact]
        public async Task CloudflareApi_GetZoneDetails_ReturnsDetailsForTheRequestedZone()
        {
            // Given
            var zoneId = "123";
            var apiToken = "456";

            _mockHttp
                .Expect($"{CloudflareApi.ENDPOINT}/zones/{zoneId}")
                .WithHeaders(new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {apiToken}" },
                })
                .RespondWithJson(new
                {
                    success = true,
                    errors = new object[] { },
                    messages = new string[] { },
                    result = new
                    {
                        id = zoneId,
                        name = "dummy"
                    }
                });

            // When
            var client = _mockHttp.ToHttpClient();
            var cloudflare = new CloudflareApi(client, new CloudflareApiOptions(apiToken));

            var zone = await cloudflare.GetZoneDetails(zoneId);

            // Then
            Assert.Equal(zoneId, zone.Id);
            Assert.Equal("dummy", zone.Name);
            _mockHttp.VerifyNoOutstandingRequest();
        }

        [Fact]
        public async Task CloudflareApi_GetZoneDetails_ThrowsAHttpExceptionForAnUnSuccessfullStatusCode()
        {
            // Given
            var zoneId = "123";
            var apiToken = "456";

            _mockHttp
                .Expect($"{CloudflareApi.ENDPOINT}/zones/{zoneId}")
                .WithHeaders(new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {apiToken}" },
                })
                .RespondWithJson(HttpStatusCode.BadRequest, new
                {
                    success = false,
                    errors = new object[] {
                        new { code = 123, message = "Something something error!"}
                    },
                    messages = new string[] { },
                });

            // When
            var client = _mockHttp.ToHttpClient();
            var cloudflare = new CloudflareApi(client, new CloudflareApiOptions(apiToken));

            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => cloudflare.GetZoneDetails(zoneId));

            // Then
            Assert.Equal(
                "Response status code does not indicate success: 400 (Bad Request).",
                exception.Message
            );
            _mockHttp.VerifyNoOutstandingRequest();
        }

        [Fact]
        public async Task CloudflareApi_ListDNSRecords_ReturnsAListOfARecordsWithoutAPager()
        {
            // Given
            var zoneId = "123";
            var apiToken = "456";
            var recordOne = new
            {
                id = "372e67954025e0ba6aaa6d586b9e0b59",
                type = "A",
                name = "yolo.example.com",
                content = "198.51.100.4",
                proxiable = true,
                proxied = false,
                ttl = 120,
                locked = false,
                zone_id = "123",
                zone_name = "example.com",
                created_on = "2014-01-01T05:20:00.12345Z",
                modified_on = "2014-01-01T05:20:00.12345Z",
            };
            var recordTwo = new
            {
                id = "99b83b442ecd4915b78050e068bd2280",
                type = "A",
                name = "whatup.example.com",
                content = "198.51.100.5",
                proxiable = true,
                proxied = false,
                ttl = 120,
                locked = false,
                zone_id = "123",
                zone_name = "example.com",
                created_on = "2014-01-01T05:20:00.12345Z",
                modified_on = "2014-01-01T05:20:00.12345Z",
            };

            _mockHttp
                .Expect($"{CloudflareApi.ENDPOINT}/zones/{zoneId}/dns_records")
                .WithQueryString(new Dictionary<string, string>
                {
                    { "type", "A" },
                    { "page", "1" },
                    { "per_page", "20" },
                })
                .WithHeaders(new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {apiToken}" },
                })
                .RespondWithJson(new
                {
                    success = true,
                    errors = new object[] { },
                    messages = new string[] { },
                    result = new object[]
                    {
                        recordOne, recordTwo,
                    },
                });

            // When
            var client = _mockHttp.ToHttpClient();
            var cloudflare = new CloudflareApi(client, new CloudflareApiOptions(apiToken));

            var (results, pager) = await cloudflare.ListDNSRecords(zoneId, "A");

            // Then
            Assert.Equal(2, results.Count);
            Assert.Collection(
                results,
                dns => dns.Should().BeEquivalentTo(new DNSResult
                {
                    Id = recordOne.id,
                    Type = recordOne.type,
                    Name = recordOne.name,
                    Content = recordOne.content,
                    Proxiable = recordOne.proxiable,
                    Proxied = recordOne.proxied,
                    Ttl = recordOne.ttl,
                    Locked = recordOne.locked,
                    ZoneId = recordOne.zone_id,
                    ZoneName = recordOne.zone_name,
                    CreatedOn = DateTimeOffset.Parse(recordOne.created_on),
                    ModifiedOn = DateTimeOffset.Parse(recordOne.modified_on),
                }),
                dns => dns.Should().BeEquivalentTo(new DNSResult
                {
                    Id = recordTwo.id,
                    Type = recordTwo.type,
                    Name = recordTwo.name,
                    Content = recordTwo.content,
                    Proxiable = recordTwo.proxiable,
                    Proxied = recordTwo.proxied,
                    Ttl = recordTwo.ttl,
                    Locked = recordTwo.locked,
                    ZoneId = recordTwo.zone_id,
                    ZoneName = recordTwo.zone_name,
                    CreatedOn = DateTimeOffset.Parse(recordTwo.created_on),
                    ModifiedOn = DateTimeOffset.Parse(recordTwo.modified_on),
                })
            );
            Assert.Null(pager);
            _mockHttp.VerifyNoOutstandingRequest();
        }

        [Fact]
        public async Task CloudflareApi_ListDNSRecords_ReturnsAnEmptyListWhenNoResultsAreReturned()
        {
            // Given
            var zoneId = "123";
            var apiToken = "456";

            _mockHttp
                .Expect($"{CloudflareApi.ENDPOINT}/zones/{zoneId}/dns_records")
                .WithQueryString(new Dictionary<string, string>
                {
                    { "type", "A" },
                    { "page", "1" },
                    { "per_page", "20" },
                })
                .WithHeaders(new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {apiToken}" },
                })
                .RespondWithJson(new
                {
                    success = true,
                    errors = new object[] { },
                    messages = new string[] { },
                });

            // When
            var client = _mockHttp.ToHttpClient();
            var cloudflare = new CloudflareApi(client, new CloudflareApiOptions(apiToken));

            var (results, pager) = await cloudflare.ListDNSRecords(zoneId, "A");

            // Then
            Assert.Empty(results);
            Assert.Null(pager);
            _mockHttp.VerifyNoOutstandingRequest();
        }

        [Fact]
        public async Task CloudflareApi_ListDNSRecords_ReturnsAPagerObject()
        {
            // Given
            var zoneId = "123";
            var apiToken = "456";
            var recordOne = new
            {
                id = "372e67954025e0ba6aaa6d586b9e0b59",
                type = "A",
                name = "yolo.example.com",
                content = "198.51.100.4",
                proxiable = true,
                proxied = false,
                ttl = 120,
                locked = false,
                zone_id = "123",
                zone_name = "example.com",
                created_on = "2014-01-01T05:20:00.12345Z",
                modified_on = "2014-01-01T05:20:00.12345Z",
            };

            _mockHttp
                .Expect($"{CloudflareApi.ENDPOINT}/zones/{zoneId}/dns_records")
                .WithQueryString(new Dictionary<string, string>
                {
                    { "page", "1" },
                    { "per_page", "20" },
                })
                .WithHeaders(new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {apiToken}" },
                })
                .RespondWithJson(new
                {
                    success = true,
                    errors = new object[] { },
                    messages = new string[] { },
                    result = new object[]
                    {
                        recordOne, recordOne, recordOne, recordOne, recordOne, recordOne
                    },
                    result_info = new
                    {
                        page = 1,
                        per_page = 20,
                        total_count = 6,
                        total_pages = 1,
                    },
                });

            // When
            var client = _mockHttp.ToHttpClient();
            var cloudflare = new CloudflareApi(client, new CloudflareApiOptions(apiToken));

            var (results, pager) = await cloudflare.ListDNSRecords(zoneId);

            // Then
            Assert.Equal(6, results.Count);
            pager.Should().BeEquivalentTo(new ApiResultPager
            {
                Page = 1,
                PerPage = 20,
                TotalCount = 6,
                TotalPages = 1,
            });
            _mockHttp.VerifyNoOutstandingRequest();
        }

        [Fact]
        public async Task CloudflareApi_ListDNSRecords_CreatesCorrectQueryForMultipleSearchParameters()
        {
            // Given
            var zoneId = "123";
            var apiToken = "456";
            var type = "CNAME";
            var name = "yolo.example.com";
            var page = 2;
            var total = 100;
            var recordOne = new
            {
                id = "372e67954025e0ba6aaa6d586b9e0b59",
                type = "A",
                name = "yolo.example.com",
                content = "198.51.100.4",
                proxiable = true,
                proxied = false,
                ttl = 120,
                locked = false,
                zone_id = "123",
                zone_name = "example.com",
                created_on = "2014-01-01T05:20:00.12345Z",
                modified_on = "2014-01-01T05:20:00.12345Z",
            };

            _mockHttp
                .Expect($"{CloudflareApi.ENDPOINT}/zones/{zoneId}/dns_records")
                .WithQueryString(new Dictionary<string, string>
                {
                    { "type", type },
                    { "name", name },
                    { "page", $"{page}" },
                    { "per_page", $"{total}" },
                })
                .WithHeaders(new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {apiToken}" },
                })
                .RespondWithJson(new
                {
                    success = true,
                    errors = new object[] { },
                    messages = new string[] { },
                    result = new object[]
                    {
                        recordOne,
                    },
                    result_info = new
                    {
                        page = 1,
                        per_page = 20,
                        total_count = 1,
                        total_pages = 1,
                    },
                });

            // When
            var client = _mockHttp.ToHttpClient();
            var cloudflare = new CloudflareApi(client, new CloudflareApiOptions(apiToken));

            var (results, pager) = await cloudflare.ListDNSRecords(zoneId, type, name, page, total);

            // Then
            _mockHttp.VerifyNoOutstandingRequest();
        }

        [Fact]
        public async Task CloudflareApi_CreateDNSRecord_CreatesANewRecord()
        {
            // Given
            var zoneId = "123";
            var apiToken = "456";
            var recordOne = new
            {
                id = "372e67954025e0ba6aaa6d586b9e0b59",
                type = "A",
                name = "yolo.example.com",
                content = "198.51.100.4",
                proxiable = true,
                proxied = false,
                ttl = 1,
                locked = false,
                zone_id = "123",
                zone_name = "example.com",
                created_on = "2014-01-01T05:20:00.12345Z",
                modified_on = "2014-01-01T05:20:00.12345Z",
            };

            _mockHttp
                .Expect($"{CloudflareApi.ENDPOINT}/zones/{zoneId}/dns_records")
                .WithHeaders(new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {apiToken}" },
                })
                .WithJsonContent(new
                {
                    type = "A",
                    name = "yolo.example.com",
                    content = "198.51.100.4",
                })
                .RespondWithJson(new
                {
                    success = true,
                    errors = new object[] { },
                    messages = new string[] { },
                    result = recordOne,
                });

            // When
            var client = _mockHttp.ToHttpClient();
            var cloudflare = new CloudflareApi(client, new CloudflareApiOptions(apiToken));

            var result = await cloudflare.CreateDNSRecord(
                zoneId,
                type: "A",
                name: "yolo.example.com",
                content: "198.51.100.4"
            );

            // Then
            result.Should().BeEquivalentTo(new DNSResult
            {
                Id = recordOne.id,
                Type = recordOne.type,
                Name = recordOne.name,
                Content = recordOne.content,
                Proxiable = recordOne.proxiable,
                Proxied = recordOne.proxied,
                Ttl = recordOne.ttl,
                Locked = recordOne.locked,
                ZoneId = recordOne.zone_id,
                ZoneName = recordOne.zone_name,
                CreatedOn = DateTimeOffset.Parse(recordOne.created_on),
                ModifiedOn = DateTimeOffset.Parse(recordOne.modified_on),
            });
            _mockHttp.VerifyNoOutstandingRequest();
        }
        [Fact]
        public async Task CloudflareApi_CreateDNSRecord_CreatesARecordWithATTLSet()
        {
            // Given
            var zoneId = "123";
            var apiToken = "456";
            var recordOne = new
            {
                id = "372e67954025e0ba6aaa6d586b9e0b59",
                type = "A",
                name = "yolo.example.com",
                content = "198.51.100.4",
                proxiable = true,
                proxied = false,
                ttl = 120,
                locked = false,
                zone_id = "123",
                zone_name = "example.com",
                created_on = "2014-01-01T05:20:00.12345Z",
                modified_on = "2014-01-01T05:20:00.12345Z",
            };

            _mockHttp
                .Expect($"{CloudflareApi.ENDPOINT}/zones/{zoneId}/dns_records")
                .WithHeaders(new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {apiToken}" },
                })
                .WithJsonContent(new
                {
                    type = "A",
                    name = "yolo.example.com",
                    content = "198.51.100.4",
                    ttl = 120,
                })
                .RespondWithJson(new
                {
                    success = true,
                    errors = new object[] { },
                    messages = new string[] { },
                    result = recordOne,
                });

            // When
            var client = _mockHttp.ToHttpClient();
            var cloudflare = new CloudflareApi(client, new CloudflareApiOptions(apiToken));

            var result = await cloudflare.CreateDNSRecord(zoneId, "A", "yolo.example.com", "198.51.100.4", 120);

            // Then
            result.Should().BeEquivalentTo(new DNSResult
            {
                Id = recordOne.id,
                Type = recordOne.type,
                Name = recordOne.name,
                Content = recordOne.content,
                Proxiable = recordOne.proxiable,
                Proxied = recordOne.proxied,
                Ttl = recordOne.ttl,
                Locked = recordOne.locked,
                ZoneId = recordOne.zone_id,
                ZoneName = recordOne.zone_name,
                CreatedOn = DateTimeOffset.Parse(recordOne.created_on),
                ModifiedOn = DateTimeOffset.Parse(recordOne.modified_on),
            });
            _mockHttp.VerifyNoOutstandingRequest();
        }

        [Fact]
        public async Task CloudflareApi_CreateDNSRecord_CreatesARecordThatsProxied()
        {
            // Given
            var zoneId = "123";
            var apiToken = "456";
            var recordOne = new
            {
                id = "372e67954025e0ba6aaa6d586b9e0b59",
                type = "A",
                name = "yolo.example.com",
                content = "198.51.100.4",
                proxiable = true,
                proxied = true,
                ttl = 120,
                locked = false,
                zone_id = "123",
                zone_name = "example.com",
                created_on = "2014-01-01T05:20:00.12345Z",
                modified_on = "2014-01-01T05:20:00.12345Z",
            };

            _mockHttp
                .Expect($"{CloudflareApi.ENDPOINT}/zones/{zoneId}/dns_records")
                .WithHeaders(new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {apiToken}" },
                })
                .WithJsonContent(new
                {
                    type = "A",
                    name = "yolo.example.com",
                    content = "198.51.100.4",
                    proxied = true,
                })
                .RespondWithJson(new
                {
                    success = true,
                    errors = new object[] { },
                    messages = new string[] { },
                    result = recordOne,
                });

            // When
            var client = _mockHttp.ToHttpClient();
            var cloudflare = new CloudflareApi(client, new CloudflareApiOptions(apiToken));

            var result = await cloudflare.CreateDNSRecord(
                zoneId,
                type: "A",
                name: "yolo.example.com",
                content: "198.51.100.4",
                proxied: true
            );

            // Then
            result.Should().BeEquivalentTo(new DNSResult
            {
                Id = recordOne.id,
                Type = recordOne.type,
                Name = recordOne.name,
                Content = recordOne.content,
                Proxiable = recordOne.proxiable,
                Proxied = recordOne.proxied,
                Ttl = recordOne.ttl,
                Locked = recordOne.locked,
                ZoneId = recordOne.zone_id,
                ZoneName = recordOne.zone_name,
                CreatedOn = DateTimeOffset.Parse(recordOne.created_on),
                ModifiedOn = DateTimeOffset.Parse(recordOne.modified_on),
            });
            _mockHttp.VerifyNoOutstandingRequest();
        }

        [Fact]
        public async Task CloudflareApi_CreateDNSRecord_ThrowsAnApiExceptionForTheFirstReturnedError()
        {
            // Given
            var zoneId = "123";
            var apiToken = "456";
            _mockHttp
                .Expect($"{CloudflareApi.ENDPOINT}/zones/{zoneId}/dns_records")
                .WithHeaders(new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {apiToken}" },
                })
                .WithJsonContent(new
                {
                    type = "A",
                    name = "yolo.example.com",
                    content = "198.51.100.4",
                    ttl = 120,
                })
                .RespondWithJson(HttpStatusCode.BadRequest, new
                {
                    success = false,
                    errors = new[]
                    {
                        new { code = 123, message = "Record already exists!"}
                    },
                    messages = new string[] { },
                });

            // When
            var client = _mockHttp.ToHttpClient();
            var cloudflare = new CloudflareApi(client, new CloudflareApiOptions(apiToken));

            var exception = await Assert.ThrowsAsync<ApiException>(() =>
                cloudflare.CreateDNSRecord(zoneId, "A", "yolo.example.com", "198.51.100.4", 120)
            );

            // Then
            Assert.Equal(123, exception.Code);
            Assert.Equal("Record already exists!", exception.Message);
            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
            _mockHttp.VerifyNoOutstandingRequest();
        }

        [Fact]
        public async Task CloudflareApi_CreateDNSRecord_ThrowsAnUnknownApiExceptionWhenResultIsNotSuccesAndNoErrorsAreReturned()
        {
            // Given
            var zoneId = "123";
            var apiToken = "456";
            _mockHttp
                .Expect($"{CloudflareApi.ENDPOINT}/zones/{zoneId}/dns_records")
                .WithHeaders(new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {apiToken}" },
                })
                .WithJsonContent(new
                {
                    type = "A",
                    name = "yolo.example.com",
                    content = "198.51.100.4",
                    ttl = 120,
                })
                .RespondWithJson(HttpStatusCode.BadRequest, new
                {
                    success = false,
                    errors = new object[] { },
                    messages = new string[] { },
                });

            // When
            var client = _mockHttp.ToHttpClient();
            var cloudflare = new CloudflareApi(client, new CloudflareApiOptions(apiToken));

            var exception = await Assert.ThrowsAsync<ApiException>(() =>
                cloudflare.CreateDNSRecord(zoneId, "A", "yolo.example.com", "198.51.100.4", 120)
            );

            // Then
            Assert.Equal(ApiError.Unknown.Code, exception.Code);
            Assert.Equal(ApiError.Unknown.Message, exception.Message);
            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
            _mockHttp.VerifyNoOutstandingRequest();
        }

        [Fact]
        public async Task CloudflareApi_UpdateDNSRecord_UpdatesAnExistingRecord()
        {
            // Given
            var zoneId = "123";
            var apiToken = "456";
            var recordOne = new
            {
                id = "372e67954025e0ba6aaa6d586b9e0b59",
                type = "A",
                name = "yolo.example.com",
                content = "198.51.100.4",
                proxiable = true,
                proxied = false,
                ttl = 120,
                locked = false,
                zone_id = "123",
                zone_name = "example.com",
                created_on = "2014-01-01T05:20:00.12345Z",
                modified_on = "2014-01-01T05:20:00.12345Z",
            };

            _mockHttp
                .Expect($"{CloudflareApi.ENDPOINT}/zones/{zoneId}/dns_records/372e67954025e0ba6aaa6d586b9e0b59")
                .WithHeaders(new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {apiToken}" },
                })
                .WithJsonContent(new
                {
                    type = "A",
                    name = "yolo.example.com",
                    content = "198.51.100.4",
                })
                .RespondWithJson(new
                {
                    success = true,
                    errors = new object[] { },
                    messages = new string[] { },
                    result = recordOne,
                });

            // When
            var client = _mockHttp.ToHttpClient();
            var cloudflare = new CloudflareApi(client, new CloudflareApiOptions(apiToken));

            var result = await cloudflare.UpdateDNSRecord(
                zoneId,
                "372e67954025e0ba6aaa6d586b9e0b59",
                type: "A",
                name: "yolo.example.com",
                content: "198.51.100.4"
            );

            // Then
            result.Should().BeEquivalentTo(new DNSResult
            {
                Id = recordOne.id,
                Type = recordOne.type,
                Name = recordOne.name,
                Content = recordOne.content,
                Proxiable = recordOne.proxiable,
                Proxied = recordOne.proxied,
                Ttl = recordOne.ttl,
                Locked = recordOne.locked,
                ZoneId = recordOne.zone_id,
                ZoneName = recordOne.zone_name,
                CreatedOn = DateTimeOffset.Parse(recordOne.created_on),
                ModifiedOn = DateTimeOffset.Parse(recordOne.modified_on),
            });
            _mockHttp.VerifyNoOutstandingRequest();
        }

        [Fact]
        public async Task CloudflareApi_UpdateDNSRecord_UpdatesAnExistingRecordWithANewTTL()
        {
            // Given
            var zoneId = "123";
            var apiToken = "456";
            var recordOne = new
            {
                id = "372e67954025e0ba6aaa6d586b9e0b59",
                type = "A",
                name = "yolo.example.com",
                content = "198.51.100.4",
                proxiable = true,
                proxied = false,
                ttl = 120,
                locked = false,
                zone_id = "123",
                zone_name = "example.com",
                created_on = "2014-01-01T05:20:00.12345Z",
                modified_on = "2014-01-01T05:20:00.12345Z",
            };

            _mockHttp
                .Expect($"{CloudflareApi.ENDPOINT}/zones/{zoneId}/dns_records/372e67954025e0ba6aaa6d586b9e0b59")
                .WithHeaders(new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {apiToken}" },
                })
                .WithJsonContent(new
                {
                    type = "A",
                    name = "yolo.example.com",
                    content = "198.51.100.4",
                    ttl = 120,
                })
                .RespondWithJson(new
                {
                    success = true,
                    errors = new object[] { },
                    messages = new string[] { },
                    result = recordOne,
                });

            // When
            var client = _mockHttp.ToHttpClient();
            var cloudflare = new CloudflareApi(client, new CloudflareApiOptions(apiToken));

            var result = await cloudflare.UpdateDNSRecord(
                zoneId,
                "372e67954025e0ba6aaa6d586b9e0b59",
                type: "A",
                name: "yolo.example.com",
                content: "198.51.100.4",
                ttl: 120
            );

            // Then
            result.Should().BeEquivalentTo(new DNSResult
            {
                Id = recordOne.id,
                Type = recordOne.type,
                Name = recordOne.name,
                Content = recordOne.content,
                Proxiable = recordOne.proxiable,
                Proxied = recordOne.proxied,
                Ttl = recordOne.ttl,
                Locked = recordOne.locked,
                ZoneId = recordOne.zone_id,
                ZoneName = recordOne.zone_name,
                CreatedOn = DateTimeOffset.Parse(recordOne.created_on),
                ModifiedOn = DateTimeOffset.Parse(recordOne.modified_on),
            });
            _mockHttp.VerifyNoOutstandingRequest();
        }

        [Fact]
        public async Task CloudflareApi_UpdateDNSRecord_UpdatesAnExistingRecordWithNewProxySetting()
        {
            // Given
            var zoneId = "123";
            var apiToken = "456";
            var recordOne = new
            {
                id = "372e67954025e0ba6aaa6d586b9e0b59",
                type = "A",
                name = "yolo.example.com",
                content = "198.51.100.4",
                proxiable = true,
                proxied = true,
                ttl = 1,
                locked = false,
                zone_id = "123",
                zone_name = "example.com",
                created_on = "2014-01-01T05:20:00.12345Z",
                modified_on = "2014-01-01T05:20:00.12345Z",
            };

            _mockHttp
                .Expect($"{CloudflareApi.ENDPOINT}/zones/{zoneId}/dns_records/372e67954025e0ba6aaa6d586b9e0b59")
                .WithHeaders(new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {apiToken}" },
                })
                .WithJsonContent(new
                {
                    type = "A",
                    name = "yolo.example.com",
                    content = "198.51.100.4",
                    proxied = true,
                })
                .RespondWithJson(new
                {
                    success = true,
                    errors = new object[] { },
                    messages = new string[] { },
                    result = recordOne,
                });

            // When
            var client = _mockHttp.ToHttpClient();
            var cloudflare = new CloudflareApi(client, new CloudflareApiOptions(apiToken));

            var result = await cloudflare.UpdateDNSRecord(
                zoneId,
                "372e67954025e0ba6aaa6d586b9e0b59",
                type: "A",
                name: "yolo.example.com",
                content: "198.51.100.4",
                proxied: true
            );

            // Then
            result.Should().BeEquivalentTo(new DNSResult
            {
                Id = recordOne.id,
                Type = recordOne.type,
                Name = recordOne.name,
                Content = recordOne.content,
                Proxiable = recordOne.proxiable,
                Proxied = recordOne.proxied,
                Ttl = recordOne.ttl,
                Locked = recordOne.locked,
                ZoneId = recordOne.zone_id,
                ZoneName = recordOne.zone_name,
                CreatedOn = DateTimeOffset.Parse(recordOne.created_on),
                ModifiedOn = DateTimeOffset.Parse(recordOne.modified_on),
            });
            _mockHttp.VerifyNoOutstandingRequest();
        }



        [Fact]
        public async Task CloudflareApi_UpdateDNSRecord_ThrowsAnApiExceptionForTheFirstReturnedError()
        {
            // Given
            var zoneId = "123";
            var apiToken = "456";
            _mockHttp
                .Expect($"{CloudflareApi.ENDPOINT}/zones/{zoneId}/dns_records/372")
                .WithHeaders(new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {apiToken}" },
                })
                .WithJsonContent(new
                {
                    type = "A",
                    name = "yolo.example.com",
                    content = "198.51.100.4",
                    ttl = 120,
                })
                .RespondWithJson(HttpStatusCode.BadRequest, new
                {
                    success = false,
                    errors = new[]
                    {
                        new { code = 123, message = "Can't update record!"}
                    },
                    messages = new string[] { },
                });

            // When
            var client = _mockHttp.ToHttpClient();
            var cloudflare = new CloudflareApi(client, new CloudflareApiOptions(apiToken));

            var exception = await Assert.ThrowsAsync<ApiException>(() =>
                cloudflare.UpdateDNSRecord(zoneId, "372", "A", "yolo.example.com", "198.51.100.4", 120)
            );

            // Then
            Assert.Equal(123, exception.Code);
            Assert.Equal("Can't update record!", exception.Message);
            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
            _mockHttp.VerifyNoOutstandingRequest();
        }

        [Fact]
        public async Task CloudflareApi_UpdateDNSRecord_ThrowsAnUnknownApiExceptionWhenResultIsNotSuccesAndNoErrorsAreReturned()
        {
            // Given
            var zoneId = "123";
            var apiToken = "456";
            _mockHttp
                .Expect($"{CloudflareApi.ENDPOINT}/zones/{zoneId}/dns_records/372")
                .WithHeaders(new Dictionary<string, string>
                {
                    { "Authorization", $"Bearer {apiToken}" },
                })
                .WithJsonContent(new
                {
                    type = "A",
                    name = "yolo.example.com",
                    content = "198.51.100.4",
                    ttl = 120,
                })
                .RespondWithJson(HttpStatusCode.BadRequest, new
                {
                    success = false,
                    errors = new object[] { },
                    messages = new string[] { },
                });

            // When
            var client = _mockHttp.ToHttpClient();
            var cloudflare = new CloudflareApi(client, new CloudflareApiOptions(apiToken));

            var exception = await Assert.ThrowsAsync<ApiException>(() =>
                cloudflare.UpdateDNSRecord(zoneId, "372", "A", "yolo.example.com", "198.51.100.4", 120)
            );

            // Then
            Assert.Equal(ApiError.Unknown.Code, exception.Code);
            Assert.Equal(ApiError.Unknown.Message, exception.Message);
            Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
            _mockHttp.VerifyNoOutstandingRequest();
        }
    }
}

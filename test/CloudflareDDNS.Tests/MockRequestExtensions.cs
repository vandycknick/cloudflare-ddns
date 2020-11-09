using System.Net;
using System.Text.Json;
using RichardSzalay.MockHttp;

namespace CloudflareDDNS.Tests
{
    public static class MockRequestExtensions
    {
        public static MockedRequest RespondWithJson<T>(this MockedRequest request, T json)
        {
            var body = JsonSerializer.Serialize<T>(json, new JsonSerializerOptions
            {
                IgnoreNullValues = true,
            });
            return request.Respond("application/json", body);
        }

        public static MockedRequest RespondWithJson<T>(this MockedRequest request, HttpStatusCode statusCode, T json)
        {
            var body = JsonSerializer.Serialize<T>(json, new JsonSerializerOptions
            {
                IgnoreNullValues = true,
            });
            return request.Respond(statusCode, "application/json", body);
        }

        public static MockedRequest WithJsonContent<T>(this MockedRequest request, T json)
        {
            var body = JsonSerializer.Serialize<T>(json, new JsonSerializerOptions
            {
                IgnoreNullValues = true,
            });
            return request.WithContent(body);
        }
    }
}

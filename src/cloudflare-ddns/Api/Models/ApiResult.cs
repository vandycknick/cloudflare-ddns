using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CloudflareDDNS.Api.Models
{
    public class ApiResult<T> where T : class
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("errors")]
        public List<ApiError> Errors { get; set; } = new List<ApiError>();

        [JsonPropertyName("messages")]
        public List<string> Messages { get; set; } = new List<string>();

        [JsonPropertyName("result")]
        public T Result { get; set; } = null!;

        [JsonPropertyName("result_info")]
        public ApiResultPager? Pager { get; set; }
    }

}

namespace CloudflareDDNS.Api.Models
{
    public class ApiError
    {
        public static readonly ApiError Unknown = new()
        {
            Code = -1,
            Message = "Unknown error.",
        };

        public int Code { get; set; }
        public string Message { get; set; } = "";
    }

}

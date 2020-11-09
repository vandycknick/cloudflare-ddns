namespace CloudflareDDNS.Api
{
    public class CloudflareApiOptions
    {
        public CloudflareApiOptions(string apiToken)
        {
            ApiToken = apiToken;
        }
        public string ApiToken { get; set; }
    }
}

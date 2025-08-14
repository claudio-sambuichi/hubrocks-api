namespace HubRocksApi.Configuration
{
    public class AppConfig
    {
        public ApiConfig Api { get; set; } = new();
        public string CacheTtlSeconds { get; set; } = string.Empty;
        public bool CachingEnabled { get; set; } = true;
        public List<string> AllowedOrigins { get; set; } = new();
    }

    public class ApiConfig
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string PageSize { get; set; } = string.Empty;
    }
}
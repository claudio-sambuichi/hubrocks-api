namespace HubRocksApi.Configuration
{
    public class AppConfig
    {
        public ApiConfig Api { get; set; } = new();
    }

    public class ApiConfig
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
    }
}
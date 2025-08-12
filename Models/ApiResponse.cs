using Newtonsoft.Json;

namespace HubRocksApi.Models
{
    public class ApiResponse
    {
        [JsonProperty("data")]
        public List<ApiCourse> Data { get; set; } = new();
        
        [JsonProperty("metadata")]
        public ApiMetadata Metadata { get; set; } = new();
    }

    public class ApiMetadata
    {
        [JsonProperty("hasNextPage")]
        public bool HasNextPage { get; set; }
    }
}
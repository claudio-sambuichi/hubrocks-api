using Newtonsoft.Json;

namespace HubRocksApi.Models
{
    public class ApiCourse
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;
        
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty; // 'MBA' | 'POS' | 'CERT'
        
        [JsonProperty("category")]
        public string Category { get; set; } = string.Empty;
        
        [JsonProperty("thumb")]
        public string Thumb { get; set; } = string.Empty;
        
        [JsonProperty("link")]
        public string Link { get; set; } = string.Empty;
        
        [JsonProperty("price")]
        public string Price { get; set; } = string.Empty;
        
        [JsonProperty("old_price")]
        public string OldPrice { get; set; } = string.Empty;
    }
}
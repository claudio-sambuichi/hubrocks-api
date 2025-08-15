namespace HubRocksApi.Models
{
    public class Course
    {
        public string id { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        public string ie { get; set; } = string.Empty;
        public string category { get; set; } = string.Empty;
        public string type { get; set; } = string.Empty;
        public string thumb { get; set; } = string.Empty;
        public string link { get; set; } = string.Empty;
        public decimal price { get; set; }
        public decimal old_price { get; set; }
    }
}
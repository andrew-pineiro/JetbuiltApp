namespace JetbuiltApp.Data
{
    public class ProductModel
    {
        public string? id { get; set; }
        public string? manufacturer { get; set; }
        public string? model { get; set; }
        public string? category_name { get; set; }
        public string? short_description { get; set; }
        public string? long_description { get; set; }
        public bool discontinued { get; set; }
        public string? msrp { get; set; }
        public string? mapp { get; set; }
        public string? part_number { get; set; }
        public string? lead_time { get; set; }
        public string? image_url { get; set; }
    }
}

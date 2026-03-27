namespace SimpleEcom.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl {get; set;} // ảnh chính
        public string? Category {get; set;}
        public int Stock {get; set;} = 0; // số lượng trong kho
        public List<ProductImage> Images {get; set;} = new List<ProductImage>();
        public List<Review> Reviews {get; set;} = new List<Review>();
        public int SoldCount {get; set;} = 0;
        public string? VideoUrl {get; set;}
    }
    public class ProductImage
    {
        public int Id {get; set;}
        public string Url {get; set;}
        public int ProductId {get; set;}
    }
}
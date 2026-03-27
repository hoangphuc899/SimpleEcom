namespace SimpleEcom.Models
{
    public class WishList
    {
        public int Id {get; set;}
        public string UserId {get; set;}
        public int ProductId {get; set;}
        public Product Product {get; set;}
    }
}
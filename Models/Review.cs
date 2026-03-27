namespace SimpleEcom.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string UserId { get; set; }
        public string FullName { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public Product Product { get; set; }
        public int OrderId {get; set;}
        public bool IsEdited {get; set;} = false; // để sửa đánh giá
        public int LikeCount {get; set;}
    }
}
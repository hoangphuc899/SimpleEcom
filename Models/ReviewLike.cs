namespace SimpleEcom.Models
{
    public class ReviewLike
    {
        public int Id {get; set;} // số thứ tự
        public int ReviewId {get; set;}
        public string UserId {get; set;}
    }
}
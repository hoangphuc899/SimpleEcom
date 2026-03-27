namespace SimpleEcom.Models
{
    public class ReportedReview
    {
        public int Id {get; set;}
        public int ReviewId {get; set;}
        public string Reason {get; set;}
        public DateTime DateReported {get; set;} = DateTime.Now;
        public bool IsResolved {get; set;} = false;
        public virtual Review Review {get; set;}
    }
}
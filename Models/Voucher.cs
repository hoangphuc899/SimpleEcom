namespace SimpleEcom.Models
{
    public class Voucher
    {
        public int Id {get; set;} // số thứ tự trong database
        public string Code {get; set;} // tên mã giảm giá
        public decimal DiscountValue {get; set;}
        public bool IsActive {get; set;} = true;
        public decimal MinOrderAmount {get; set;}
        public DateTime ExpiryDate {get; set;}
        public bool IsPercent {get; set;}
        public decimal? MaxDiscount {get; set;}
        public int UsageLimit {get; set;} = 1;
        public int UsedCount {get; set;} = 0;
    }
}
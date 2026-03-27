namespace SimpleEcom.Models
{
    public class Order // bảng thông tin khách hàng mua sản phẩm
    {
        public int Id {get; set;}
        public string CustomerName {get; set;} = "";
        public string Phone {get; set;} = "";
        public string Address {get; set;} = "";
        public decimal TotalAmount {get; set;}
        public DateTime? OrderDate {get; set;} = DateTime.Now;
        public string? OrderItemsJson {get; set;}
        public string? Status {get; set;}
        public string? UserId {get; set;} // để khách hàng theo dõi đơn
        public DateTime? DateCompleted {get; set;}
        public DateTime? DateCancelled {get; set;}
        public string? CancellationReason {get; set;}
        public decimal DiscountAmount {get; set;} = 0; // lưu số tiền đã giảm
        public string? VoucherCode {get; set;}
    }
}
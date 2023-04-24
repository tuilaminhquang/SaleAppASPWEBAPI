using System;
namespace saleapp.Models
{
    public enum StatusEnum
    {
        WaitingShipper,
        ToReceive,
        Completed
    }

    public enum PayMentMethod
    {
        Momo,
        COD
    }

    public class Order: BaseModel
	{
        public int Id { get; set; }
        public User Customer { get; set; }
        public User? Shipper { get; set; }
        public string ShipAddress { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public StatusEnum Status { get; set; }
        public PayMentMethod PaymentMethod { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; }
    }
}


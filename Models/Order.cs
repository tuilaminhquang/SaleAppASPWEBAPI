using System;
namespace saleapp.Models
{
    public enum StatusEnum
    {
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
        public Customer Customer { get; set; }
        public string ShipAddress { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public StatusEnum Status { get; set; }
        public PayMentMethod PaymentMethod { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; }
    }
}


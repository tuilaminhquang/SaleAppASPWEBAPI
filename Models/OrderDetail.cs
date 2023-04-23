using System;
namespace saleapp.Models
{

    public class OrderDetail : BaseModel
    {
        public int Id { get; set; }
        public Order Order { get; set; }
        public Product Product { get; set; }
        public int Quantity { get; set; }
        public decimal Discount { get; set; }
        public float Price { get; set; }
    }
}


using System;
namespace saleapp.Models
{
    public class Product: BaseModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public float Price { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; }

    }

}


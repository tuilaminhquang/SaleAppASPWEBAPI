using System;
namespace saleapp.Models
{
	public class Customer: BaseModel
	{
        public int Id { get; set; }
        public string Phone { get; set; }
        public User User { get; set; }
    }
}


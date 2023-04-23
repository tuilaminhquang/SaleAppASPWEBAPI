using System;
namespace saleapp.Models
{
	public class Shipper: BaseModel
	{
		public int Id { get; set; }
		public User User { get; set; }
	}
}


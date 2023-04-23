using System;
using System.ComponentModel.DataAnnotations;
namespace saleapp.DTO
{
	public class CategoryDTO
    {
        [Required]
        public string Name { get; set; }
    }

}


using System;
namespace saleapp.DTO
{
    public class ProductCreateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public float Price { get; set; }
        public int CategoryId { get; set; }
        public IFormFile ImageFile { get; set; }
    }

}


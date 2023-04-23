using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using saleapp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using saleapp.DTO;
namespace saleapp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Product
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts(int? pageNumber, int? pageSize, int? CateId, string? search)
        {
            var products = await _context.Products.ToListAsync();
            if (CateId != null)
            {
                products = products.Where(p => p.CategoryId == CateId).ToList();
            }
            if (!string.IsNullOrEmpty(search))
            {
                //products = products.Where(p => p.Name.Contains(search)).ToList();

                //insensitive case
                products = products.Where(p => p.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            }

            if (pageNumber == null || pageSize == null)
            {
                products.ForEach(p => p.ImageUrl = "https://localhost:7097/images/product/" + p.ImageUrl);
                return products;
            }

            int currentPage = pageNumber.Value;
            int currentPageSize = pageSize.Value;
            int totalItems = products.Count();

            var items = products.Skip((currentPage - 1) * currentPageSize).Take(currentPageSize).ToList();
            // modify imageUrl property for each product
            items.ForEach(p => p.ImageUrl = "https://localhost:7097/images/product/" + p.ImageUrl);

            return Ok(new
            {
                PageNumber = currentPage,
                PageSize = currentPageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)currentPageSize),
                Items = items
            });
        }


        // GET: api/Product/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.ImageUrl = "https://localhost:7097/images/product/" + product.ImageUrl;

            }
            if (product == null)
            {
                return NotFound();
            }

            return product;
        }

        // PUT: api/Product/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.Id)
            {
                return BadRequest();
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Product
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromForm] ProductCreateDto model)
        {
            var product = new Product
            {
                Name = model.Name,
                Title = model.Title,
                Description = model.Description,
                Price = model.Price,
                CategoryId = model.CategoryId
            };

            // Save the image to the server
            if (model.ImageFile != null)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "images/product", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(stream);
                }

                product.ImageUrl = fileName;
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return Ok(product);
        }



        // DELETE: api/Product/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}

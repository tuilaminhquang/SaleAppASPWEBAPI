using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using saleapp.Models;
using saleapp.DTO;
using Microsoft.AspNetCore.Identity;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace saleapp.Controllers
{
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly ApplicationDbContext _context;


        public OrderController(ApplicationDbContext context)
        {
            _context = context;

        }
        // GET: api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateOrder([FromBody] OrderDTO orderDTO)
        {
            // Get the current user
            var userId = User.FindFirst(ClaimTypes.Name)?.Value;
            var customer = await _context.Users.FindAsync(userId);
            var cartProducts = orderDTO.Products;

            // Create the order
            if (customer == null)
            {
                return NotFound($"User with id {userId} not found.");
            }
            var order = new Order
            {
                Customer = customer,
                CreatedDate = DateTime.UtcNow,
                Status = StatusEnum.WaitingShipper,
                PaymentMethod = orderDTO.PaymentMethod,
                ShipAddress = orderDTO.ShipAddress,
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Create the order details
            foreach (var cartProduct in cartProducts)
            {
                var product = await _context.Products.FindAsync(cartProduct.Id);
                if (product == null)
                {
                    return BadRequest($"Product with id {cartProduct.Id} not found.");
                }

                var orderDetail = new OrderDetail
                {
                    Order = order,
                    Product = product,
                    Quantity = cartProduct.Quantity,
                    Discount = 0, // You can calculate the discount if needed
                    Price = product.Price // You can calculate the price if needed
                };

                _context.OrderDetails.Add(orderDetail);
            }

            await _context.SaveChangesAsync();

            // Project the Order object to an anonymous type or a DTO
            var result = new
            {
                Id = order.Id,
                CreatedDate = order.CreatedDate,
                UpdatedDate = order.UpdatedDate,
                Status = order.Status,
                PaymentMethod = order.PaymentMethod,
                ShipAddress = order.ShipAddress,
                customerId = customer.Id,
            };

            return Ok(result);

        }

        [HttpGet]
        [Authorize]
        [Route("my-order")]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetOrdersByUser()
        {
            // Get the current user
            var userId = User.FindFirst(ClaimTypes.Name)?.Value;

            // Get all orders where the customer is the current user
            if (userId == null)
            {
                return NotFound();
            }
            //Use Include when need load related
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.Customer.Id == userId)
                .ToListAsync();


            // Convert the orders to a response
            var response = orders.Select(o => new 
            {
                Id = o.Id,
                ShipAddress = o.ShipAddress,
                CreatedDate = o.CreatedDate,
                UpdatedDate = o.UpdatedDate,
                Status = o.Status.ToString(),
                PaymentMethod = o.PaymentMethod.ToString(),
                Products = o.OrderDetails?.Select(od => new
                {
                    Id = od.Product?.Id,
                    ProductName = od.Product?.Name ?? "",
                    Price = od.Product?.Price,
                    Quantity = od.Quantity,
                }).ToList()

            }).ToList();

            return Ok(response);
        }


        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}


﻿using System;
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
using System.Text.Json;
using System.Text.Json.Serialization;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace saleapp.Controllers
{
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;



        public OrderController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
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
            var user = await _context.Users.FindAsync(userId);
            var roles = await _userManager.GetRolesAsync(user);
            List<Order> orders;

            // Get all orders where the customer is the current user
            if (userId == null)
            {
                return NotFound();
            }
            if (roles.Contains("Shipper"))
            {
                orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.Shipper.Id == userId)
                .ToListAsync();
            }
            else
            {
                //Use Include when need load related
            
                orders = await _context.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                    .Where(o => o.Customer.Id == userId)
                    .ToListAsync();

            }

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

        [HttpGet]
        [Authorize]
        [Route("waiting")]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetAllWaitingOrder()
        {
            // Get the current user
            var userId = User.FindFirst(ClaimTypes.Name)?.Value;
            var user = await _context.Users.FindAsync(userId);
            var roles = await _userManager.GetRolesAsync(user);
            bool canAccess = roles.Contains("Shipper") | roles.Contains("Admin");
            if (userId == null)
            {
                return NotFound();
            }
            if (!canAccess)
            {
                return Forbid();
            }
            //Use Include when need load related
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.Status == StatusEnum.WaitingShipper)
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

        [HttpPatch("{orderId}/pick-up")]
        [Authorize]
        public async Task<IActionResult> UpdateOrderStatusToReceive(int orderId)
        {
            // Find the order by ID
            var userId = User.FindFirst(ClaimTypes.Name)?.Value;
            var user = await _context.Users.FindAsync(userId);
            var roles = await _userManager.GetRolesAsync(user);
            var order = await _context.Orders.Include(o => o.Shipper).FirstOrDefaultAsync(o => o.Id == orderId);

            if (!roles.Contains("Shipper"))
            {
                return Forbid();
            }

            // Check if the order exists
            if (order == null)
            {
                return NotFound();
            }

            // Update the order status to ToCeive when shipper pickup this order
            order.Status = StatusEnum.ToReceive;
            order.Shipper = user;

            // Save changes to the database
            await _context.SaveChangesAsync();

            // Return a success response
            return Ok();
        }

        [HttpPatch("{orderId}/mark-complete")]
        [Authorize]
        public async Task<IActionResult> UpdateOrderStatus(int orderId)
        {
            // Find the order by ID
            var userId = User.FindFirst(ClaimTypes.Name)?.Value;
            var user = await _context.Users.FindAsync(userId);
            var roles = await _userManager.GetRolesAsync(user);
            var order = await _context.Orders.Include(o => o.Shipper).FirstOrDefaultAsync(o => o.Id == orderId);

            bool is_shipper_of_order = order?.Shipper?.Id == userId;
            if (!roles.Contains("Admin") && !is_shipper_of_order)
            {
                return Forbid();
            }
            
            // Check if the order exists
            if (order == null)
            {
                return NotFound();
            }

            // Update the order status
            order.Status = StatusEnum.Completed;

            // Save changes to the database
            await _context.SaveChangesAsync();

            // Return a success response
            return Ok();
        }

        [HttpPatch("{orderId}/delete")]
        [Authorize]
        public async Task<IActionResult> DeleteOrderById(int orderId)
        {
            // Find the order by ID
            var userId = User.FindFirst(ClaimTypes.Name)?.Value;
            var user = await _context.Users.FindAsync(userId);
            var roles = await _userManager.GetRolesAsync(user);

            if (!roles.Contains("Admin"))
            {
                return Forbid();
            }
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet]
        [Authorize]
        [Route("admin")]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetAllOrder(StatusEnum? status)
        {
            var query = _context.Orders.AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status);
            }

            var orders = await query
                .Include(o => o.Shipper)
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .ToListAsync();

            // Convert the orders to a response
            var response = orders.Select(o => new
            {
                Id = o.Id,
                Customer = o.Customer,
                Shipper = o.Shipper,
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


    }
}


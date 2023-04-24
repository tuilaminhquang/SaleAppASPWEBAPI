using System;
using System.ComponentModel.DataAnnotations;
using saleapp.Models;

namespace saleapp.DTO
{
    public class OrderDTO
    {
        [Required]
        public string ShipAddress { get; set; }

        [Required]
        public PayMentMethod PaymentMethod { get; set; }

        [Required]
        public List<CartProductDTO> Products { get; set; }
    }
}


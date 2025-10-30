using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManager.API.Application.DTOs
{
    public class SaleItemResponseDTO
    {
        public int ProductId { get; set; } // <-- Mudança
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
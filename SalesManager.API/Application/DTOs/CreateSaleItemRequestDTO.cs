using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManager.API.Application.DTOs
{
    public class CreateSaleItemRequestDTO
    {
        public int ProductId { get; set; } // <-- Mudança
        public int Quantity { get; set; }
    }
}
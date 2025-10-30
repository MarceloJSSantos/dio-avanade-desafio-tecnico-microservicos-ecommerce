using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManager.API.Application.DTOs
{
    // DTO para criar uma Venda (Request)
    public class CreateSaleRequestDTO
    {
        public int CustomerId { get; set; } // <-- Mudança
        public required List<CreateSaleItemRequestDTO> Items { get; set; }
    }
}
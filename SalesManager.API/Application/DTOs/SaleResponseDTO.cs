using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManager.API.Application.DTOs
{
    // DTO para exibir uma Venda (Response)
    public class SaleResponseDTO
    {
        public int Id { get; set; } // <-- Mudança
        public required string Status { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public required List<SaleItemResponseDTO> Items { get; set; }
    }
}
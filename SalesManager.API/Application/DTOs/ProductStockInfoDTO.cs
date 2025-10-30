using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManager.API.Application.DTOs
{
    // DTO de resposta do StockManager (também atualizado)
    public class ProductStockInfoDTO
    {
        public int ProductId { get; set; } // <-- Mudança
        public required string ProductName { get; set; }
        public string? ProductDescription { get; set; }
        public decimal UnitPrice { get; set; }
        public int StockQuantity { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StockManager.API.Domain.DTOs
{
    public class UpdateStockResponseDTO
    {
        public int ProductId { get; set; }
        public int TransactionAmount { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StockManager.API.Domain.DTOs
{
    public record UpdateStockDTO
    {
        public int TransactionAmount { get; set; }
    }
}
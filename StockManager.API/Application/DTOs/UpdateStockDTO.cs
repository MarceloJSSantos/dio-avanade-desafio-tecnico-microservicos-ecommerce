using System.ComponentModel.DataAnnotations;

namespace StockManager.API.Application.DTOs
{
    public record UpdateStockDTO
    {
        [Required]
        [Range(-10000, 10000, ErrorMessage = "O valor permitido deve estar entre {1} e {2}.")]
        public int TransactionAmount { get; init; }
    }
}
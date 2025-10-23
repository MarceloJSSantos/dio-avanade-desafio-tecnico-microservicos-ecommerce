namespace StockManager.API.Domain.DTOs
{
    public record UpdateStockDTO
    {
        public int TransactionAmount { get; set; }
    }
}
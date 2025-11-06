namespace SalesManager.API.Application.DTOs
{
    public class SaleItemResponseDTO
    {
        public int ProductId { get; set; } // <-- Mudança
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
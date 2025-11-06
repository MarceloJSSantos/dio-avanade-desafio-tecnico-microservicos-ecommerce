namespace SalesManager.API.Application.DTOs
{
    public class CreateSaleItemRequestDTO
    {
        public int ProductId { get; set; } // <-- Mudança
        public int Quantity { get; set; }
    }
}
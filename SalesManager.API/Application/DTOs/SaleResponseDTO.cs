namespace SalesManager.API.Application.DTOs
{
    public class SaleResponseDTO
    {
        public int Id { get; set; }
        public required string Status { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
        public required List<SaleItemResponseDTO> Items { get; set; }
    }
}
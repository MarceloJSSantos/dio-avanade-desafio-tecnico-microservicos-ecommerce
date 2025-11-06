namespace SalesManager.API.Application.DTOs
{
    public record SaleResponseDTO
    {
        public int Id { get; init; }

        public required string Status { get; init; }

        public decimal TotalPrice { get; init; }
        public DateTime CreatedAt { get; init; }

        public required List<SaleItemResponseDTO> Items { get; init; }
    }
}
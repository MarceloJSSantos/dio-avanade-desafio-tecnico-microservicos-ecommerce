namespace StockManager.API.Application.DTOs
{
    public record ProductResponseDTO
    {
        public int ProductId { get; init; }
        public string Name { get; init; }

        public string Description { get; init; }

        public decimal Price { get; init; }
        public int QuantityInStock { get; init; }
    }
}
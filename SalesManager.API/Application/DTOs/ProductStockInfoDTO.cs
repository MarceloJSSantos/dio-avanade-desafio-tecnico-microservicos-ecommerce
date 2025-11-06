using System.Text.Json.Serialization;

namespace SalesManager.API.Application.DTOs
{
    public record ProductStockInfoDTO
    {
        public int ProductId { get; init; }

        [JsonPropertyName("Name")]
        public required string ProductName { get; init; }

        public string? ProductDescription { get; init; }

        [JsonPropertyName("Price")]
        public decimal UnitPrice { get; init; }

        [JsonPropertyName("QuantityInStock")]
        public int StockQuantity { get; init; }
    }
}
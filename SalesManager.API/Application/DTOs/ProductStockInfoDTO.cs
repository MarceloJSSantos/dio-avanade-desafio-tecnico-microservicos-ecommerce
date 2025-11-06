using System.Text.Json.Serialization;

namespace SalesManager.API.Application.DTOs
{
    public class ProductStockInfoDTO
    {
        public int ProductId { get; set; }
        [JsonPropertyName("Name")]
        public required string ProductName { get; set; }
        public string? ProductDescription { get; set; }
        [JsonPropertyName("Price")]
        public decimal UnitPrice { get; set; }
        [JsonPropertyName("QuantityInStock")]
        public int StockQuantity { get; set; }
    }
}
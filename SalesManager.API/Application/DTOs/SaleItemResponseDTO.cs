namespace SalesManager.API.Application.DTOs
{
    public record SaleItemResponseDTO(int ProductId, int Quantity, decimal UnitPrice);
}
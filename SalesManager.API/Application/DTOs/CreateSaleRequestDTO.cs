using SalesManager.API.Domain.Enums;

namespace SalesManager.API.Application.DTOs
{
    public record CreateSaleRequestDTO
    {
        public int CustomerId { get; init; }

        public List<CreateSaleItemRequestDTO> Items { get; init; } = new();

        public SaleStatus? InitialStatus { get; init; }
    }
}
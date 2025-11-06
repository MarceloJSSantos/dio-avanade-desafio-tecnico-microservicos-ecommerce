using SalesManager.API.Domain.Enums;

namespace SalesManager.API.Application.DTOs
{
    public class CreateSaleRequestDTO
    {
        public int CustomerId { get; set; }
        public List<CreateSaleItemRequestDTO> Items { get; set; } = new();

        public SaleStatus? InitialStatus { get; set; }
    }
}
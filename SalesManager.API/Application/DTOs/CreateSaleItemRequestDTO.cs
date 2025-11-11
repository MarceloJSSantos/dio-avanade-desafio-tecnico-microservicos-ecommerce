using System.ComponentModel.DataAnnotations;

namespace SalesManager.API.Application.DTOs
{
    public record CreateSaleItemRequestDTO
    {
        [Required(ErrorMessage = "ProductId é obrigatório")]
        [Range(1, int.MaxValue, ErrorMessage = "ProductId deve ser maior que zero.")]
        public int ProductId { get; init; }

        [Required(ErrorMessage = "Quantity é obrigatório")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity deve ser maior que zero.")]
        public int Quantity { get; init; }
    }
}
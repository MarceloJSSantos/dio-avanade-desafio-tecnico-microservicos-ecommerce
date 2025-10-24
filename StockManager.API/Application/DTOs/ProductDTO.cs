using System.ComponentModel.DataAnnotations;

namespace StockManager.API.Application.DTOs
{
    public record ProductDTO
    {
        [Required(ErrorMessage = "O nome do produto é obrigatório.")]
        [StringLength(150, ErrorMessage = "O nome deve ter no máximo {1} caracteres.")]
        public string Name { get; set; }

        [StringLength(1000, ErrorMessage = "A descrição deve ter no máximo {1} caracteres.")]
        public string Description { get; set; }

        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "O preço deve ser maior que zero.")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "A quantidade em estoque deve ser maior que zero.")]
        public int QuantityInStock { get; set; }
    }
}
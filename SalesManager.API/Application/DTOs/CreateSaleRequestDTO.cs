using SalesManager.API.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SalesManager.API.Application.DTOs
{
    public class CreateSaleRequestDTO : IValidatableObject
    {
        [Required(ErrorMessage = "CustomerId é obrigatório")]
        [Range(1, int.MaxValue, ErrorMessage = "CustomerId deve ser maior que zero.")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Items é obrigatório e deve conter ao menos um item.")]
        [MinLength(1, ErrorMessage = "Ao menos 1 item é obrigatório.")]
        public List<CreateSaleItemRequestDTO> Items { get; set; } = new();

        public SaleStatus? InitialStatus { get; init; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // 1. Validação de InitialStatus
            if (InitialStatus.HasValue)
            {
                // Verifica se o valor do enum é válido.
                if (!Enum.IsDefined(typeof(SaleStatus), InitialStatus.Value))
                {
                    yield return new ValidationResult(
                        $"O status inicial fornecido é inválido.",
                        new[] { nameof(InitialStatus) }
                    );
                }
            }

            if (Items == null || !Items.Any())
            {
                yield return new ValidationResult("A venda deve conter ao menos um item.", new[] { nameof(Items) });
                yield break;
            }

            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                var results = new List<ValidationResult>();
                var ctx = new ValidationContext(item, validationContext, null);
                if (!Validator.TryValidateObject(item, ctx, results, validateAllProperties: true))
                {
                    foreach (var r in results)
                    {
                        yield return new ValidationResult(r.ErrorMessage, new[] { $"{nameof(Items)}[{i}].{(r.MemberNames.FirstOrDefault() ?? string.Empty)}" });
                    }
                }
            }
        }
    }
}
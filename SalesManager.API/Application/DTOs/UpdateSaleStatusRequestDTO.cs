using SalesManager.API.Domain.Enums;
using System.ComponentModel.DataAnnotations;

public class UpdateSaleStatusRequestDTO
{
    [Required]
    public SaleStatus NewStatus { get; set; }
}
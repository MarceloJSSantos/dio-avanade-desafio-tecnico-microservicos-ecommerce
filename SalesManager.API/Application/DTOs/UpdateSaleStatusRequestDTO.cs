using SalesManager.API.Domain.Enums;
using System.ComponentModel.DataAnnotations;

public record UpdateSaleStatusRequestDTO([Required] SaleStatus NewStatus);
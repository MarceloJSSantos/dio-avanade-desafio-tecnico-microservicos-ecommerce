using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SalesManager.API.Domain.Enums;

namespace SalesManager.API.Application.DTOs
{
    public record UpdateSaleStatusRequestDTO
    {
        [Required(ErrorMessage = "NewStatus é obrigatório")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [EnumDataType(typeof(SaleStatus))]
        public SaleStatus? NewStatus { get; init; }
    }
}
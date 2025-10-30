using AutoMapper;
using SalesManager.API.Application.DTOs;
using SalesManager.API.Domain.Entities;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Mapeamento da Entidade de Domínio -> DTO de Resposta
        CreateMap<Sale, SaleResponseDTO>()
            // Converte o Enum SaleStatus para seu nome em string
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<SaleItem, SaleItemResponseDTO>();

        // Nota: Não estamos mapeando DTOs de Request -> Entidades
        // porque a criação da entidade 'Sale' envolve lógica de negócio 
        // (chamar o construtor, validar, etc.) que é melhor tratada
        // explicitamente dentro do 'SaleService'.
    }
}
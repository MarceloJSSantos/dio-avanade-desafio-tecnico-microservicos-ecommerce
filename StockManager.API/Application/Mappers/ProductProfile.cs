using AutoMapper;
using StockManager.API.Application.DTOs;
using StockManager.API.Domain.Entities;

namespace StockManager.API.Application.Mappers
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            CreateMap<ProductDTO, Product>()
                .ForMember(destino => destino.ProductId, opt => opt.Ignore());

            CreateMap<Product, ProductResponseDTO>();
        }
    }
}
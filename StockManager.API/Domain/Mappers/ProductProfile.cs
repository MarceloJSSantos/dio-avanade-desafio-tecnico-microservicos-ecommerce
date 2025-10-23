using AutoMapper;
using StockManager.API.Domain.DTOs;
using StockManager.API.Domain.Entities;

namespace StockManager.API.Domain.Mappers
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
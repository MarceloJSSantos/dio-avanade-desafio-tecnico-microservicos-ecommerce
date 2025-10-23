using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using StockManager.API.Domain.DTOs;
using StockManager.API.Domain.Entities;

namespace StockManager.API.Domain.Mappers
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            // Mapeamento de DTO de Requisição para Entidade
            // Ex: Create(ProductDTO) -> Entidade Produto
            CreateMap<ProductDTO, Product>()
                // Mapeamentos específicos podem ser adicionados aqui
                // Por exemplo, ignorar um campo que só existe na Entidade
                .ForMember(destino => destino.ProductId, opt => opt.Ignore());

            // Mapeamento de Entidade para DTO de Resposta (Se você tiver um)
            // Ex: Entidade Produto -> ResponseDTO
            CreateMap<Product, ProductResponseDTO>();

            //Não utilizado
            // Mapeamento para o DTO de atualização de estoque, se necessário
            // (Embora para o PATCH de delta, o mapeamento DTO -> Entidade seja menos comum)
            CreateMap<UpdateStockDTO, UpdateStockResponseDTO>();
        }
    }
}
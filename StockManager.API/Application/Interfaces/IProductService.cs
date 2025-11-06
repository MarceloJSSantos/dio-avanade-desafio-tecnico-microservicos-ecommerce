using StockManager.API.Application.Common;
using StockManager.API.Domain.Entities;
using StockManager.API.Application.DTOs;

namespace StockManager.API.Application.Interfaces
{
    public interface IProductService
    {
        Task<Product> CreateProductAsync(Product product);

        Task<Product> GetProductByIdAsync(int id);

        Task<int> UpdateStockAsync(int productId, int transactionAmount);

        Task<PagedResult<ProductResponseDTO>> GetPagedProductsAsync(int page, int pageSize);
    }
}
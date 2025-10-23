using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StockManager.API.Domain.DTOs;
using StockManager.API.Domain.Entities;

namespace StockManager.API.Domain.Interfaces
{
    public interface IProductService
    {
        Task<Product> CreateProductAsync(Product product);

        Task<IEnumerable<Product>> GetAllProductsAsync();

        Task<Product> GetProductByIdAsync(int id);

        Task<int> UpdateStockAsync(int products, int transactionAmount);
    }
}
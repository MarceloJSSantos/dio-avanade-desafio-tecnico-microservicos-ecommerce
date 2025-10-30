using SalesManager.API.Application.DTOs;

namespace SalesManager.API.Application.Interfaces
{
    public interface IStockManagerClient
    {
        Task<ProductStockInfoDTO?> GetProductStockAsync(int productId); // <-- Mudança
        Task<bool> DecreaseStockAsync(int productId, int quantity); // <-- Mudança
        Task<bool> IncreaseStockAsync(int productId, int quantity); // <-- Mudança
    }
}
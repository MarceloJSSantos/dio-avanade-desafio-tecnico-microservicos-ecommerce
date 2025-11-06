using SalesManager.API.Application.DTOs;

namespace SalesManager.API.Application.Interfaces
{
    public interface IStockManagerClient
    {
        Task<ProductStockInfoDTO?> GetProductStockAsync(int productId);
        Task<bool> DecreaseStockAsync(int productId, int quantity);
        Task<bool> IncreaseStockAsync(int productId, int quantity);
    }
}
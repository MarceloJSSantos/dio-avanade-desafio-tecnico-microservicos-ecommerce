using SalesManager.API.Application.Common;
using SalesManager.API.Application.DTOs;

namespace SalesManager.API.Application.Interfaces
{
    public interface ISaleService
    {
        Task<SaleResponseDTO> CreateSaleAsync(CreateSaleRequestDTO request);
        Task<SaleResponseDTO> GetSaleByIdAsync(int id);
        Task<bool> CancelSaleAsync(int saleId);
        Task<PagedResult<SaleResponseDTO>> GetSalesAsync(int pageNumber, int pageSize);
        Task<SaleResponseDTO> UpdateSaleStatusAsync(int saleId, UpdateSaleStatusRequestDTO request);
    }
}
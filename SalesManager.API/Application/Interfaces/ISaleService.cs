using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SalesManager.API.Application.DTOs;

namespace SalesManager.API.Application.Interfaces
{
    public interface ISaleService
    {
        Task<SaleResponseDTO> CreateSaleAsync(CreateSaleRequestDTO request);
        Task<SaleResponseDTO> GetSaleByIdAsync(int id);
        Task<bool> CancelSaleAsync(int saleId);
        // NOVO MÉTODO: Retorna uma lista de SaleResponse
        Task<IEnumerable<SaleResponseDTO>> GetSalesAsync(int pageNumber, int pageSize);
    }
}
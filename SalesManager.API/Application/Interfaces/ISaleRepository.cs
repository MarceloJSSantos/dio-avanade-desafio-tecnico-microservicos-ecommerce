using SalesManager.API.Application.DTOs;
using SalesManager.API.Domain.Entities;

namespace SalesManager.API.Application.Interfaces
{
    public interface ISaleRepository
    {
        Task<Sale?> GetByIdAsync(int id);
        Task<IEnumerable<Sale>> GetByCustomerIdAsync(int customerId);
        Task AddAsync(Sale sale);
        Task UpdateAsync(Sale sale);
        // NOVO MÉTODO: Retorna uma lista paginada de vendas
        Task<IEnumerable<Sale>> GetSalesAsync(int pageNumber, int pageSize);
    }
}
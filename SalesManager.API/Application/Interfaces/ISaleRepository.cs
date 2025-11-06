using SalesManager.API.Application.Common;
using SalesManager.API.Domain.Entities;

namespace SalesManager.API.Application.Interfaces
{
    public interface ISaleRepository
    {
        Task<Sale?> GetByIdAsync(int id);
        Task<IEnumerable<Sale>> GetByCustomerIdAsync(int customerId);
        Task AddAsync(Sale sale);
        Task UpdateAsync(Sale sale);
        Task<PagedResult<Sale>> GetSalesAsync(int pageNumber, int pageSize);
    }
}
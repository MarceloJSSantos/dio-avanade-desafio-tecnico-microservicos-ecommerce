using Microsoft.EntityFrameworkCore;
using SalesManager.API.Application.Common;
using SalesManager.API.Application.Interfaces;
using SalesManager.API.Domain.Entities;
using SalesManager.API.Infrastructure.Db;

namespace SalesManager.API.Domain.Repositories
{
    public class SaleRepository : ISaleRepository
    {
        private readonly SalesDbContext _context;

        public SaleRepository(SalesDbContext context)
        {
            _context = context;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task AddAsync(Sale sale)
        {
            await _context.Sales.AddAsync(sale);
        }

        public async Task<IEnumerable<Sale>> GetByCustomerIdAsync(int customerId)
        {
            return await _context.Sales
            .Include(s => s.Items)
            .Where(s => s.CustomerId == customerId)
            .ToListAsync();
        }

        public async Task<Sale?> GetByIdAsync(int id)
        {
            return await _context.Sales
                .Include(s => s.Items) // Carregar os itens juntos
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task UpdateAsync(Sale sale)
        {
            _context.Sales.Update(sale);
        }

        public async Task<PagedResult<Sale>> GetSalesAsync(int pageNumber, int pageSize)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;

            var query = _context.Sales.AsQueryable();

            int totalCount = await query.CountAsync();

            int skip = (pageNumber - 1) * pageSize;

            var sales = await query
                .Include(s => s.Items)
                // Ordenar (essencial para que a paginação funcione de forma consistente)
                .OrderByDescending(s => s.CreatedAt)
                // Pular a quantidade de registros da página anterior
                .Skip(skip)
                // Pegar a quantidade de registros da página atual
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Sale>(sales, totalCount);
        }
    }
}
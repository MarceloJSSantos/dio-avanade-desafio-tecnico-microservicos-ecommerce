using Microsoft.EntityFrameworkCore;
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

        public async Task AddAsync(Sale sale)
        {
            await _context.Sales.AddAsync(sale);
            await _context.SaveChangesAsync();
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
            // return new Sale(15000000);
            return await _context.Sales
                .Include(s => s.Items) // Carregar os itens juntos
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task UpdateAsync(Sale sale)
        {
            // Garante que a entidade está sendo rastreada como modificada
            _context.Sales.Update(sale);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Retorna uma lista paginada de todas as vendas, ordenadas pela data de criação.
        /// </summary>
        public async Task<IEnumerable<Sale>> GetSalesAsync(int pageNumber, int pageSize)
        {
            // Cálculo do 'Skip' para implementar a paginação:
            // Ex: pageNumber=2, pageSize=10 -> Skip = (2-1) * 10 = 10 (pula os 10 primeiros)
            int skip = (pageNumber - 1) * pageSize;

            return await _context.Sales
                // 1. Incluir os itens para evitar problemas de N+1
                .Include(s => s.Items)
                // 2. Ordenar (essencial para que a paginação funcione de forma consistente)
                .OrderByDescending(s => s.CreatedAt)
                // 3. Pular a quantidade de registros da página anterior
                .Skip(skip)
                // 4. Pegar a quantidade de registros da página atual
                .Take(pageSize)
                .ToListAsync();
        }
    }
}
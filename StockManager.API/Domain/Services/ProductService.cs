using Microsoft.EntityFrameworkCore;
using StockManager.API.Domain.Entities;
using StockManager.API.Domain.Interfaces;
using StockManager.API.Infrastructure.Db;

namespace StockManager.API.Domain.Services
{
    public class ProductService : IProductService
    {
        private readonly StockManagerContext _context;

        public ProductService(StockManagerContext context)
        {
            _context = context;
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _context.Products.ToListAsync();
        }

        public async Task<Product> GetProductByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<int> UpdateStockAsync(int productId, int transactionAmount)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var product = await _context.Products
                                            .FirstOrDefaultAsync(p => p.ProductId == productId);

                if (product == null)
                {
                    throw new KeyNotFoundException($"Produto com ID {productId} não encontrado.");
                }

                if (product.QuantityInStock + transactionAmount < 0)
                {
                    throw new InvalidOperationException("Não há estoque suficiente para esta operação.");
                }

                product.QuantityInStock += transactionAmount;

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return product.QuantityInStock;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
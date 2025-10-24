using Microsoft.EntityFrameworkCore;
using StockManager.API.Domain.Entities;
using StockManager.API.Application.Interfaces;
using StockManager.API.Infrastructure.Db;

namespace StockManager.API.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly StockManagerContext _context;
        private readonly ILogger<ProductService> _logger;

        public ProductService(StockManagerContext context, ILogger<ProductService> logger)
        {
            _context = context;
            _logger = logger;
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

                _logger.LogInformation("Estoque atualizado. ProductId={ProductId} NewStock={NewStock}", productId, product.QuantityInStock);

                return product.QuantityInStock;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar estoque para ProductId={ProductId}", productId);
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
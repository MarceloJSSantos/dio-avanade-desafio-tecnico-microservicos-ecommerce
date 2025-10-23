using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        // READ ALL
        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _context.Products.ToListAsync();
        }

        // READ BY ID
        public async Task<Product> GetProductByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        // UPDATE STOCK (Com a lógica PATCH / Delta)
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

                // 1. Validação de Estoque (Evita Estoque Negativo)
                if (product.QuantityInStock + transactionAmount < 0)
                {
                    throw new InvalidOperationException("Não há estoque suficiente para esta operação.");
                }

                // 2. Atualizar o Saldo
                product.QuantityInStock += transactionAmount;

                // 4. Salvar as alterações (Produto e Movimentação)
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return product.QuantityInStock; // Retorna o novo saldo
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw; // Relança a exceção para ser tratada na Controller
            }
        }
    }
}
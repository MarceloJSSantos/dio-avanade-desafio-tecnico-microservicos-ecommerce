using Microsoft.EntityFrameworkCore;
using StockManager.API.Domain.Entities;
using StockManager.API.Application.Interfaces;
using StockManager.API.Infrastructure.Db;
using StockManager.API.Application.Common;
using StockManager.API.Application.DTOs;
using AutoMapper;
using SalesManager.API.Application.Events;
using MassTransit;

namespace StockManager.API.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly StockManagerContext _context;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<ProductService> _logger;
        private readonly IMapper _mapper;

        public ProductService(StockManagerContext context,
                    IPublishEndpoint publishEndpoint,
                    ILogger<ProductService> logger,
                    IMapper mapper)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            _logger.LogInformation(">>> Creating product '{Name}'", product.Name);
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            _logger.LogInformation(">>> Product created with id '{ProductId}'", product.ProductId);
            return product;
        }

        public async Task<Product> GetProductByIdAsync(int id)
        {
            _logger.LogDebug(">>> Fetching product by id '{Id}'", id);
            return await _context.Products.FindAsync(id);
        }

        public async Task<int> UpdateStockAsync(int productId, int transactionAmount)
        {
            _logger.LogInformation(">>> Updating stock: ProductId='{ProductId}' Amount='{Amount}'", productId, transactionAmount);
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);

                if (product == null)
                {
                    _logger.LogWarning(">>> Product not found when updating stock. ProductId='{ProductId}'", productId);
                    throw new KeyNotFoundException($"Produto com ID {productId} não encontrado.");
                }

                if (product.QuantityInStock + transactionAmount < 0)
                {
                    _logger.LogWarning(">>> Insufficient stock: ProductId='{ProductId}' CurrentStock='{Stock}' Transaction='{Tx}'", productId, product.QuantityInStock, transactionAmount);
                    throw new InvalidOperationException("Não há estoque suficiente para esta operação.");
                }

                product.QuantityInStock += transactionAmount;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(">>> Stock updated successfully. ProductId='{ProductId}' NewStock='{NewStock}'", productId, product.QuantityInStock);
                return product.QuantityInStock;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ">>> Error updating stock for ProductId='{ProductId}'", productId);
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<PagedResult<ProductResponseDTO>> GetPagedProductsAsync(int page, int pageSize)
        {
            const int MaxPageSize = 100;
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;
            pageSize = Math.Min(pageSize, MaxPageSize);

            _logger.LogInformation(">>> Getting paged products page='{Page}' pageSize='{PageSize}'", page, pageSize);
            var query = _context.Products.AsNoTracking().OrderBy(p => p.ProductId);
            var total = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var productDtos = _mapper.Map<IEnumerable<ProductResponseDTO>>(items);

            return new PagedResult<ProductResponseDTO>
            {
                Items = productDtos,
                TotalCount = total
            };
        }

        public async Task<bool> UpdateStockBatchAsync(List<SaleItemMessage> items, bool isDeduction)
        {
            _logger.LogInformation(">>> Starting Batch Stock Update. ItemsCount={Count}, IsDeduction={IsDeduction}", items.Count, isDeduction);
            foreach (var item in items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);

                if (product == null)
                {
                    _logger.LogWarning(">>> Batch Update Failed: Product {ProductId} not found.", item.ProductId);
                    return false;
                }

                int changeAmount = isDeduction ? -item.Quantity : item.Quantity;
                if (product.QuantityInStock + changeAmount < 0)
                {
                    _logger.LogWarning(">>> Batch Update Failed: Insufficient stock for {ProductId}. Current: {Current}, Requested: {Requested}",
                                     item.ProductId, product.QuantityInStock, item.Quantity);
                    return false;
                }

                product.QuantityInStock += changeAmount;
            }

            _logger.LogInformation(">>> Batch Stock Update Committed Successfully.");
            return true;
        }
    }
}
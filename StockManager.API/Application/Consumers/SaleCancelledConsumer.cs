using MassTransit;
using SalesManager.API.Application.Events;
using StockManager.API.Application.Interfaces;
using StockManager.API.Infrastructure.Db;

namespace StockManager.API.Application.Consumers;

public class SaleCancelledConsumer : IConsumer<SaleCancelledEvent>
{
    private readonly StockManagerContext _dbContext;
    private readonly IProductService _productService;
    private readonly ILogger<SaleCancelledConsumer> _logger;

    public SaleCancelledConsumer(
        StockManagerContext dbContext,
        IProductService productService,
        ILogger<SaleCancelledConsumer> logger)
    {
        _dbContext = dbContext;
        _productService = productService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SaleCancelledEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation(">>> StockManager: Returning stock from Sale {SaleId} (Cancelled)", message.SaleId);

        bool success = await _productService.UpdateStockBatchAsync(message.Items, isDeduction: false);

        if (success)
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation(">>> SUCCESS: Stock returned to Sale {SaleId}", message.SaleId);
        }
        else
        {
            _logger.LogError(">>> ERROR: Unable to return stock for Sale {SaleId}", message.SaleId);
        }
    }
}
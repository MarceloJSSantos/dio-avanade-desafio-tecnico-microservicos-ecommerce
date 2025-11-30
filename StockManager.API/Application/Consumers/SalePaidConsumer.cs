using MassTransit;
using SalesManager.API.Application.Events;
using StockManager.API.Application.Interfaces;
using StockManager.API.Infrastructure.Db;

namespace StockManager.API.Application.Consumers;

public class SalePaidConsumer : IConsumer<SalePaidEvent>
{
    private readonly StockManagerContext _dbContext;
    private readonly IProductService _productService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<SalePaidConsumer> _logger;

    public SalePaidConsumer(
        StockManagerContext dbContext,
        IProductService productService,
        IPublishEndpoint publishEndpoint,
        ILogger<SalePaidConsumer> logger)
    {
        _dbContext = dbContext;
        _productService = productService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SalePaidEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation(">>> StockManager: Received SalePaidEvent for Sale {SaleId}", message.SaleId);

        try
        {
            bool success = await _productService.UpdateStockBatchAsync(context.Message.Items, true);

            if (success)
            {
                await context.Publish(new StockSuccessEvent(context.Message.SaleId, DateTime.UtcNow));
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation(">>> Complete success.");
            }
            else
            {
                await context.Publish(new StockErrorEvent(context.Message.SaleId, "Estoque insuficiente"));
                await _dbContext.SaveChangesAsync();
                _logger.LogError(">>> Batch failed. Posting StockErrorEvent.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, ">>> CRITICAL CONSUME FAILURE FOR SALE {SaleId}. Message will be retried/moved to error queue.", message.SaleId);
            throw;
        }

    }
}
using MassTransit;
using SalesManager.API.Application.Events;
using SalesManager.API.Application.Interfaces;

namespace SalesManager.API.Application.Consumers;

// Este Consumer escuta o erro vindo do StockManager
public class StockErrorConsumer : IConsumer<StockErrorEvent>
{
    private readonly ISaleRepository _saleRepository;
    private readonly ILogger<StockErrorConsumer> _logger;

    public StockErrorConsumer(ISaleRepository saleRepository, ILogger<StockErrorConsumer> logger)
    {
        _saleRepository = saleRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockErrorEvent> context)
    {
        var message = context.Message;
        _logger.LogWarning(">>> SalesManager: Received StockErrorEvent for Sale {SaleId}. Stock deduction failed. Initiating compensation.", message.SaleId);

        try
        {
            var sale = await _saleRepository.GetByIdAsync(message.SaleId);

            if (sale == null)
            {
                _logger.LogError(">>> COMPENSATION FAILURE: Sale {SaleId} not found during StockError processing.", message.SaleId);
                return;
            }

            sale.SetStatusToCancel();

            await _saleRepository.UpdateAsync(sale);
            await _saleRepository.SaveChangesAsync();

            _logger.LogInformation(">>> SalesManager: Compensation complete. Sale {SaleId} reverted to {Status}. Reason: {Reason}",
                message.SaleId, sale.Status, message.Reason);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "!!! FATAL ERROR in StockErrorConsumer for Sale {SaleId}. Checking inner exception.", message.SaleId);
            throw;
        }

    }
}
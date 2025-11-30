using MassTransit;
using SalesManager.API.Application.Events;
using SalesManager.API.Application.Interfaces;

namespace SalesManager.API.Application.Consumers
{
    public class StockResponseConsumer :
        IConsumer<StockSuccessEvent>,
        IConsumer<StockErrorEvent>
    {
        private readonly ISaleRepository _saleRepository;
        private readonly ILogger<StockResponseConsumer> _logger;

        public StockResponseConsumer(ISaleRepository saleRepository, ILogger<StockResponseConsumer> logger)
        {
            _saleRepository = saleRepository;
            _logger = logger;
        }

        // Cenário 1: Estoque processou tudo bem
        public Task Consume(ConsumeContext<StockSuccessEvent> context)
        {
            _logger.LogInformation(">>> FEEDBACK RECEBIDO: Estoque confirmado para Venda {SaleId}", context.Message.SaleId);
            //Retirado o async da assinatura e só concluído
            return Task.CompletedTask;
        }

        // Cenário 2: Deu erro no Estoque (Ex: Furo de estoque concorrente)
        public async Task Consume(ConsumeContext<StockErrorEvent> context)
        {
            _logger.LogError(">>> ERROR FEEDBACK: Stock failure for Sale {SaleId}. Reason: {Reason}", context.Message.SaleId, context.Message.Reason);
            var sale = await _saleRepository.GetByIdAsync(context.Message.SaleId);

            if (sale != null)
            {
                // COMPENSAÇÃO AUTOMÁTICA
                // O estoque falhou, então cancelamos a venda ou marcamos como "Erro de Processamento"
                sale.SetStatusToCancel();
                // Adicionar lógica de estorno financeiro aqui se necessário
                await _saleRepository.UpdateAsync(sale);
                _logger.LogWarning(">>> Sale {SaleId} automatically cancelled due to stock error.", sale.Id);
            }
        }
    }
}
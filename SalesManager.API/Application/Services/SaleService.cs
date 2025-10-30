using AutoMapper;
using SalesManager.API.Application.DTOs;
using SalesManager.API.Application.Interfaces;
using SalesManager.API.Domain.Entities;
using SalesManager.API.Domain.Enums; // <-- Adicionado

public class SaleService : ISaleService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IStockManagerClient _stockManagerClient;
    private readonly IMapper _mapper; // <-- Adicionado

    public SaleService(
        ISaleRepository saleRepository,
        IStockManagerClient stockManagerClient,
        IMapper mapper) // <-- Adicionado
    {
        _saleRepository = saleRepository;
        _stockManagerClient = stockManagerClient;
        _mapper = mapper; // <-- Adicionado
    }

    public async Task<SaleResponseDTO> CreateSaleAsync(CreateSaleRequestDTO request)
    {
        // 1. Criar a entidade de Venda
        var sale = new Sale(request.CustomerId); // <-- Usa 'int'

        // 2. Para cada item, verificar estoque e pegar preço
        foreach (var itemRequest in request.Items)
        {
            var productInfo = await _stockManagerClient.GetProductStockAsync(itemRequest.ProductId); // <-- Usa 'int'

            if (productInfo == null)
                throw new Exception($"Produto {itemRequest.ProductId} não encontrado.");

            if (productInfo.StockQuantity < itemRequest.Quantity)
                throw new Exception($"Estoque insuficiente para {productInfo.ProductName}.");

            sale.AddItem(itemRequest.ProductId, itemRequest.Quantity, productInfo.UnitPrice);
        }

        // 3. Salvar a Venda (Status: PendingPayment)
        sale.CalculateTotalPrice();
        await _saleRepository.AddAsync(sale);

        // 4. (Simulação) Pagamento aprovado e baixa de estoque
        sale.SetStatusToPaid();
        foreach (var item in sale.Items)
        {
            await _stockManagerClient.DecreaseStockAsync(item.ProductId, item.Quantity);
        }

        await _saleRepository.UpdateAsync(sale);

        // 8. Retornar DTO de resposta usando AutoMapper
        return _mapper.Map<SaleResponseDTO>(sale); // <-- Mudança: Mapeamento automático
    }

    // ... (GetSaleByIdAsync, CancelSaleAsync com 'int' como parâmetro) ...
    public async Task<SaleResponseDTO> GetSaleByIdAsync(int id)
    {
        var sale = await _saleRepository.GetByIdAsync(id);
        return _mapper.Map<SaleResponseDTO>(sale); // <-- Mapeamento automático
    }

    // TODO implementar o cancelamento da venda (inverso 'CreateSaleAsync')
    public async Task<bool> CancelSaleAsync(int saleId)
    {
        // 1. Buscar a Venda
        var sale = await _saleRepository.GetByIdAsync(saleId);

        if (sale == null)
        {
            // Não encontramos a venda, não podemos cancelar.
            return false;
        }

        // 2. Aplicar Regra de Negócio Local (Validação)
        // Regra: Não se pode cancelar vendas que já foram concluídas ou já estão canceladas.
        if (sale.Status == SaleStatus.Completed || sale.Status == SaleStatus.Cancelled)
        {
            // A venda não está em um status que permite cancelamento.
            return false;
        }

        // 3. Compensação: Devolver os Itens ao Estoque (Comunicação Externa)
        // Este é um passo crucial de compensação de transação.
        bool stockIncreaseSuccess = true;

        // Só devolvemos o estoque se a venda já tinha sido paga e o estoque abatido.
        if (sale.Status != SaleStatus.PendingPayment)
        {
            foreach (var item in sale.Items)
            {
                // Chama o StockManager.API para aumentar o estoque
                var success = await _stockManagerClient.IncreaseStockAsync(
                    item.ProductId,
                    item.Quantity
                );

                if (!success)
                {
                    // Se falhar a devolução de UM item, interrompemos e registramos a falha.
                    // Em um ambiente de produção real, isso dispararia um Evento de Compensação 
                    // para ser re-tentado (padrão SAGA).
                    stockIncreaseSuccess = false;
                    break;
                }
            }
        }

        if (!stockIncreaseSuccess)
        {
            // Se falhou ao compensar o estoque, podemos optar por não cancelar a venda 
            // e deixar o status atual (ex: Paid) para que um operador resolva manualmente.
            // Para este exemplo, retornamos false.
            // Você pode querer logar um erro aqui.
            return false;
        }

        // 4. Atualizar o Status Local da Venda
        try
        {
            sale.Cancel(); // Chama o método de domínio para definir o Status = Cancelled
            await _saleRepository.UpdateAsync(sale);

            // 5. Sucesso
            return true;
        }
        catch (Exception)
        {
            // Se falhar o SaveChanges (ex: problema de DB), o estoque já foi devolvido. 
            // Isso gera inconsistência! (É por isso que SAGA é melhor).
            // Aqui você logaria uma FALHA CRÍTICA e dispararia uma notificação.
            return false;
        }
    }
    /// <summary>
    /// Obtém uma lista paginada de vendas e as mapeia para DTOs de resposta.
    /// </summary>
    public async Task<IEnumerable<SaleResponseDTO>> GetSalesAsync(int pageNumber, int pageSize)
    {
        // 1. Aplicar validações básicas na paginação, se necessário
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20; // Limitar o tamanho da página

        // 2. Chamar o repositório para obter as Entidades de Domínio
        var sales = await _saleRepository.GetSalesAsync(pageNumber, pageSize);

        // 3. Mapear a lista de Entidades para uma lista de DTOs de Resposta
        return _mapper.Map<IEnumerable<SaleResponseDTO>>(sales);
    }
}
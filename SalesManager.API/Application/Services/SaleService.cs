using AutoMapper;
using SalesManager.API.Application.Common;
using SalesManager.API.Application.DTOs;
using SalesManager.API.Application.Interfaces;
using SalesManager.API.Domain.Entities;
using SalesManager.API.Domain.Enums;

public class SaleService : ISaleService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IStockManagerClient _stockManagerClient;
    private readonly IMapper _mapper;

    public SaleService(
        ISaleRepository saleRepository,
        IStockManagerClient stockManagerClient,
        IMapper mapper)
    {
        _saleRepository = saleRepository;
        _stockManagerClient = stockManagerClient;
        _mapper = mapper;
    }

    public async Task<SaleResponseDTO> CreateSaleAsync(CreateSaleRequestDTO request)
    {
        var sale = new Sale(request.CustomerId);

        foreach (var itemRequest in request.Items)
        {
            var productInfo = await _stockManagerClient.GetProductStockAsync(itemRequest.ProductId); // <-- Usa 'int'

            if (productInfo == null)
                throw new Exception($"Produto com Id '{itemRequest.ProductId}' não encontrado.");

            await ValidateItems(productInfo, itemRequest);

            sale.AddItem(itemRequest.ProductId, itemRequest.Quantity, productInfo.UnitPrice);
        }

        sale.CalculateTotalPrice();

        if (request.InitialStatus.HasValue)
        {
            var requested = request.InitialStatus.Value;
            switch (requested)
            {
                case SaleStatus.PendingPayment:
                    break;
                case SaleStatus.Paid:
                    await SetStatusToPaid(sale);
                    break;
                case SaleStatus.Shipped:
                    await SetStatusToPaid(sale);
                    sale.SetStatusToShipped();
                    break;
                default:
                    throw new InvalidOperationException($"Status '{requested}' como inicial é inválido.");
            }
        }

        await _saleRepository.AddAsync(sale);
        await _saleRepository.UpdateAsync(sale);

        return _mapper.Map<SaleResponseDTO>(sale);
    }

    public async Task<SaleResponseDTO> GetSaleByIdAsync(int id)
    {
        var sale = await _saleRepository.GetByIdAsync(id);
        if (sale == null)
        {
            throw new Exception($"Venda com Id '{id}' não encontrada.");
        }
        return _mapper.Map<SaleResponseDTO>(sale);
    }

    public async Task<bool> CancelSaleAsync(int saleId)
    {
        var sale = await _saleRepository.GetByIdAsync(saleId);

        if (sale == null)
        {
            throw new Exception($"Venda com Id '{saleId}' não encontrada.");
        }

        // Regra: Não se pode cancelar vendas que já foram enviadas, concluídas ou já estão canceladas.
        if (sale.Status == SaleStatus.Shipped || sale.Status == SaleStatus.Completed || sale.Status == SaleStatus.Cancelled)
        {
            // A venda não está em um status que permite cancelamento.
            throw new Exception($"A Venda com Id '{saleId}' está com o status '{sale.Status}', o qual não permite cancelamento.");
        }

        // Compensação: Devolver os Itens ao Estoque (Comunicação Externa)
        // Este é um passo crucial de compensação de transação.
        // Estudar a melhor maneira de fazer essa compensação
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
                    // Estudar a melhor maneira de fazer essa compensação
                    stockIncreaseSuccess = false;
                    break;
                }
            }
        }

        if (!stockIncreaseSuccess)
        {
            // Se falhou ao compensar o estoque, podemos optar por não cancelar a venda 
            // e deixar o status atual (ex: Paid) para que um operador resolva manualmente.
            throw new Exception($"Houve uma falha ao tentar cancelar a venda com Id '{saleId}' ao tentar compensar o estoque. Faça manualmente!");

        }

        try
        {
            sale.SetStatusToCancel();
            await _saleRepository.UpdateAsync(sale);

            return true;
        }
        catch (Exception)
        {
            // Se falhar o SaveChanges (ex: problema de DB), o estoque já foi devolvido. 
            // Isso gera inconsistência! (É por isso que SAGA é melhor).
            // Aqui você logaria uma FALHA CRÍTICA e dispararia uma notificação.
            // Estudar a melhor maneira de fazer essa compensação
            Console.WriteLine("[FALHA CRÍTICA] - Implementar SAGA");
            return false;
        }
    }

    public async Task<PagedResult<SaleResponseDTO>> GetSalesAsync(int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var pagedSalesEntity = await _saleRepository.GetSalesAsync(pageNumber, pageSize);

        if (pagedSalesEntity == null || pagedSalesEntity.TotalCount == 0)
        {
            return new PagedResult<SaleResponseDTO>(new List<SaleResponseDTO>(), 0);
        }

        var saleDtos = _mapper.Map<IEnumerable<SaleResponseDTO>>(pagedSalesEntity.Items);

        var pagedResultDto = new PagedResult<SaleResponseDTO>(
            saleDtos,
            pagedSalesEntity.TotalCount
        );

        return pagedResultDto;
    }

    public async Task<SaleResponseDTO> UpdateSaleStatusAsync(int saleId, UpdateSaleStatusRequestDTO request)
    {
        var sale = await _saleRepository.GetByIdAsync(saleId);
        if (sale == null)
        {
            throw new Exception($"Venda com Id '{saleId}' não encontrada.");
        }

        var currentStatus = sale.Status;
        var newStatus = request.NewStatus;

        if (newStatus == currentStatus)
        {
            return _mapper.Map<SaleResponseDTO>(sale);
        }

        // Não fazer o cancelamento por essa rota, usar a própria
        if (newStatus == SaleStatus.Cancelled)
        {
            throw new InvalidOperationException($"Transição inválida: Não é permitido mudar para '{SaleStatus.Cancelled}' por esta rota. Use a rota adequada.");
        }

        // Validação de Pré-condição (Evitar retrocesso de status e pulos inválidos)
        if ((int)newStatus < (int)currentStatus)
        {
            throw new InvalidOperationException($"Transição inválida: Não é permitido mudar de '{currentStatus}' para '{newStatus}'.");
        }

        // Executar Ações de Transição de Status
        switch (newStatus)
        {
            case SaleStatus.Paid:
                // Regra: Só pode ir para Pago se estiver Pendente
                if (currentStatus != SaleStatus.PendingPayment)
                {
                    throw new InvalidOperationException($"Venda de Id '{saleId}' e com status '{currentStatus}' só pode ser marcada como '{newStatus}' se estiver como '{SaleStatus.PendingPayment}'.");
                }
                await SetStatusToPaid(sale); // Abate estoque e marca como Pago
                break;

            case SaleStatus.Shipped:
                // Regra: Só pode ser Enviado se já estiver Pago
                if (currentStatus != SaleStatus.Paid)
                {
                    throw new InvalidOperationException($"Venda de Id '{saleId}' e com status '{currentStatus}' só pode ser marcada como '{newStatus}' se estiver como '{SaleStatus.Paid}'.");
                }
                sale.SetStatusToShipped(); // Supondo que você criou este método de domínio
                break;

            case SaleStatus.Completed:
                // Regra: Só pode ser Concluído se estiver Enviado
                if (currentStatus != SaleStatus.Shipped)
                {
                    throw new InvalidOperationException($"Venda de Id '{saleId}' e com status '{currentStatus}' só  pode ser marcada como '{newStatus}' após ser '{SaleStatus.Shipped}'.");
                }
                sale.SetStatusToCompleted();
                break;

            case SaleStatus.PendingPayment:
                throw new InvalidOperationException($"Transição inválida: Não é permitido mudar para '{newStatus}'.");

            default:
                throw new ArgumentException($"Status de venda '{newStatus}' inválido ou não implementado.");
        }

        // Salvar as alterações no banco de dados
        await _saleRepository.UpdateAsync(sale);

        return _mapper.Map<SaleResponseDTO>(sale);
    }

    private async Task SetStatusToPaid(Sale sale)
    {
        sale.SetStatusToPaid();
        // TODO: Rever aqui, pois pode ter alterado o estoque entre 'PendingPayment' e 'Paid'
        //       gerando inconsistência
        foreach (var item in sale.Items)
        {
            await _stockManagerClient.DecreaseStockAsync(item.ProductId, item.Quantity);
        }
    }
    private Task ValidateItems(ProductStockInfoDTO productInfo, CreateSaleItemRequestDTO itemRequest)
    {
        if (itemRequest.Quantity < 0)
            throw new Exception($"A quantidade (Item: '{itemRequest.ProductId}', quantidade: '{itemRequest.Quantity}') não pode ser menor que zero.");

        if (productInfo.StockQuantity < itemRequest.Quantity)
            throw new Exception($"Estoque insuficiente para '{productInfo.ProductName}'.");

        return Task.CompletedTask;
    }
}
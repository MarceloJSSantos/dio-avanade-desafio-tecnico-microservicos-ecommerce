using AutoMapper;
using SalesManager.API.Application.Common;
using SalesManager.API.Application.DTOs;
using SalesManager.API.Application.Interfaces;
using SalesManager.API.Domain.Entities;
using SalesManager.API.Domain.Enums;
using MassTransit;
using SalesManager.API.Application.Events;

public class SaleService : ISaleService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IStockManagerClient _stockManagerClient;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IMapper _mapper;
    private readonly ILogger<SaleService> _logger;

    public SaleService(
        ISaleRepository saleRepository,
        IStockManagerClient stockManagerClient,
        IPublishEndpoint publishEndpoint,
        IMapper mapper,
        ILogger<SaleService> logger)
    {
        _saleRepository = saleRepository;
        _stockManagerClient = stockManagerClient;
        _publishEndpoint = publishEndpoint;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<SaleResponseDTO> CreateSaleAsync(CreateSaleRequestDTO request)
    {
        _logger.LogInformation(">>> Creating sale for CustomerId='{CustomerId}' ItemsCount='{ItemsCount}'", request.CustomerId, request.Items?.Count ?? 0);
        var sale = new Sale(request.CustomerId);

        foreach (var itemRequest in request.Items!)
        {
            ProductStockInfoDTO? productInfo;

            try
            {
                productInfo = await _stockManagerClient.GetProductStockAsync(itemRequest.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ">>> Critical failure while querying stock for product '{ProductId}'", itemRequest.ProductId);
                throw new InvalidOperationException($"Serviço de estoque indisponível. Não foi possível procurar pelo produto '{itemRequest.ProductId}'. Tente novamente mais tarde.");
            }

            if (productInfo == null)
            {
                _logger.LogWarning(">>> Product '{ProductId}' not registered in stock.", itemRequest.ProductId);
                throw new KeyNotFoundException($"Produto com Id '{itemRequest.ProductId}' não encontrado.");
            }

            if (productInfo.StockQuantity < itemRequest.Quantity)
            {
                _logger.LogWarning(">>> Insufficient stock for product '{ProductId}'.", itemRequest.ProductId);
                throw new InvalidOperationException($"Estoque insuficiente para o produto com Id '{itemRequest.ProductId}' - '{productInfo.ProductName}'. Solicitado: {itemRequest.Quantity}, Disponível: {productInfo.StockQuantity}");
            }

            sale.AddItem(itemRequest.ProductId, itemRequest.Quantity, productInfo.UnitPrice);
        }

        sale.CalculateTotalPrice();

        if (request.InitialStatus.HasValue)
        {
            var requested = request.InitialStatus.Value;
            _logger.LogInformation(">>> Applying initial status '{Status}' to Sale (CustomerId='{CustomerId}')", requested, request.CustomerId);
            switch (requested)
            {
                case SaleStatus.PendingPayment:
                    break;
                case SaleStatus.Paid:
                    sale.SetStatusToPaid();
                    await PublishSalePaidEvent(sale);
                    break;
                case SaleStatus.Shipped:
                    sale.SetStatusToPaid();
                    await PublishSalePaidEvent(sale);
                    sale.SetStatusToShipped();
                    break;
                default:
                    _logger.LogWarning(">>> Invalid initial status requested: {Status}", requested);
                    throw new InvalidOperationException($"Status '{requested}' como inicial é inválido.");
            }
        }

        await _saleRepository.AddAsync(sale);
        await _saleRepository.SaveChangesAsync();

        _logger.LogInformation(">>> Sale created with Id='{SaleId}' CustomerId='{CustomerId}' Total='{Total}'", sale.Id, sale.CustomerId, sale.TotalPrice);
        return _mapper.Map<SaleResponseDTO>(sale);
    }

    public async Task<SaleResponseDTO> GetSaleByIdAsync(int id)
    {
        _logger.LogInformation(">>> Fetching sale by Id='{SaleId}'", id);
        var sale = await _saleRepository.GetByIdAsync(id);
        if (sale == null)
        {
            _logger.LogWarning(">>> Sale not found. Id='{SaleId}'", id);
            throw new KeyNotFoundException($"Venda com Id '{id}' não encontrada.");
        }

        _logger.LogDebug(">>> Sale fetched. Id='{SaleId}' ItemsCount='{ItemsCount}' Total='{Total}'", sale.Id, sale.Items?.Count ?? 0, sale.TotalPrice);
        return _mapper.Map<SaleResponseDTO>(sale);
    }

    public async Task<bool> CancelSaleAsync(int saleId)
    {
        _logger.LogInformation(">>> CancelSale requested. SaleId='{SaleId}'", saleId);
        var sale = await _saleRepository.GetByIdAsync(saleId);

        if (sale == null)
        {
            _logger.LogWarning(">>> CancelSale failed: not found. SaleId='{SaleId}'", saleId);
            throw new KeyNotFoundException($"Venda com Id '{saleId}' não encontrada.");
        }

        if (sale.Status == SaleStatus.Shipped || sale.Status == SaleStatus.Completed || sale.Status == SaleStatus.Cancelled)
        {
            _logger.LogWarning(">>> CancelSale invalid state. SaleId='{SaleId}' Status='{Status}'", saleId, sale.Status);
            throw new InvalidOperationException($"A Venda com Id '{saleId}' está com o status '{sale.Status}', o qual não permite cancelamento.");
        }

        sale.SetStatusToCancel();

        if (sale.Items != null && sale.Items.Any())
        {
            var eventMessage = new SaleCancelledEvent
            {
                SaleId = sale.Id,
                CancelledAt = DateTime.UtcNow,
                Items = sale.Items.Select(i => new SaleItemMessage(i.ProductId, i.Quantity)).ToList()
            };

            await _publishEndpoint.Publish(eventMessage);
            _logger.LogInformation(">>> The SaleCancelledEvent event has been queued in Outbox for SaleId='{SaleId}'", saleId);
        }

        await _saleRepository.UpdateAsync(sale);
        await _saleRepository.SaveChangesAsync();

        _logger.LogInformation(">>> Sale cancelled successfully. SaleId='{SaleId}'", saleId);
        return true;
    }

    public async Task<PagedResult<SaleResponseDTO>> GetSalesAsync(int pageNumber, int pageSize)
    {
        _logger.LogInformation(">>> GetSalesAsync paged request: pageNumber={PageNumber} pageSize={PageSize}", pageNumber, pageSize);

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var pagedSalesEntity = await _saleRepository.GetSalesAsync(pageNumber, pageSize);

        var saleDtos = _mapper.Map<IEnumerable<SaleResponseDTO>>(pagedSalesEntity.Items);

        _logger.LogInformation(">>> GetSalesAsync returned {Count} items", saleDtos?.Count() ?? 0);
        var pagedResultDto = new PagedResult<SaleResponseDTO>(
            saleDtos!,
            pagedSalesEntity.TotalCount
        );

        return pagedResultDto;
    }

    public async Task<SaleResponseDTO> UpdateSaleStatusAsync(int saleId, UpdateSaleStatusRequestDTO request)
    {
        _logger.LogInformation(">>> UpdateSaleStatus requested. SaleId='{SaleId}' NewStatus='{NewStatus}'", saleId, request.NewStatus);

        var sale = await _saleRepository.GetByIdAsync(saleId);
        if (sale == null)
        {
            _logger.LogWarning(">>> UpdateSaleStatus failed: not found. SaleId='{SaleId}'", saleId);
            throw new KeyNotFoundException($"Venda com Id '{saleId}' não encontrada.");
        }

        var currentStatus = sale.Status;
        var newStatus = request.NewStatus;

        if (newStatus == SaleStatus.Cancelled)
        {
            _logger.LogWarning(">>> UpdateSaleStatus invalid attempt to set Cancelled via this route. SaleId='{SaleId}'", saleId);
            throw new InvalidOperationException($"Transição inválida: Não é permitido mudar para '{SaleStatus.Cancelled}' por esta rota. Use a rota adequada.");
        }

        if ((int?)newStatus <= (int)currentStatus)
        {
            _logger.LogWarning(">>> UpdateSaleStatus invalid transition from {Current} to {New}. SaleId='{SaleId}'", currentStatus, newStatus, saleId);
            throw new InvalidOperationException($"Transição inválida: Não é permitido mudar de '{currentStatus}' para '{newStatus}'.");
        }

        switch (newStatus)
        {
            case SaleStatus.Paid:
                if (currentStatus != SaleStatus.PendingPayment)
                {
                    _logger.LogWarning(">>> UpdateSaleStatus invalid current state for Paid. SaleId='{SaleId}' CurrentStatus='{CurrentStatus}'", saleId, currentStatus);
                    throw new InvalidOperationException($"Venda de Id '{saleId}' e com status '{currentStatus}' só pode ser marcada como '{newStatus}' se estiver como '{SaleStatus.PendingPayment}'.");
                }

                sale.SetStatusToPaid();
                await PublishSalePaidEvent(sale);
                break;

            case SaleStatus.Shipped:
                if (currentStatus != SaleStatus.Paid)
                {
                    _logger.LogWarning(">>> UpdateSaleStatus invalid current state for Shipped. SaleId='{SaleId}' CurrentStatus='{CurrentStatus}'", saleId, currentStatus);
                    throw new InvalidOperationException($"Venda de Id '{saleId}' e com status '{currentStatus}' só pode ser marcada como '{newStatus}' se estiver como '{SaleStatus.Paid}'.");
                }
                sale.SetStatusToShipped();
                break;

            case SaleStatus.Completed:
                if (currentStatus != SaleStatus.Shipped)
                {
                    _logger.LogWarning(">>> UpdateSaleStatus invalid current state for Completed. SaleId='{SaleId}' CurrentStatus='{CurrentStatus}'", saleId, currentStatus);
                    throw new InvalidOperationException($"Venda de Id '{saleId}' e com status '{currentStatus}' só  pode ser marcada como '{newStatus}' após ser '{SaleStatus.Shipped}'.");
                }
                sale.SetStatusToCompleted();
                break;

            case SaleStatus.PendingPayment:
                _logger.LogWarning(">>> UpdateSaleStatus attempt to revert to PendingPayment. SaleId='{SaleId}'", saleId);
                throw new InvalidOperationException($"Transição inválida: Não é permitido mudar para '{newStatus}'.");

            default:
                _logger.LogError(">>> UpdateSaleStatus unsupported status requested: {RequestedStatus} for SaleId='{SaleId}'", newStatus, saleId);
                throw new ArgumentException($"Status de venda '{newStatus}' inválido ou não implementado.");
        }

        await _saleRepository.UpdateAsync(sale);
        await _saleRepository.SaveChangesAsync();

        _logger.LogInformation(">>> UpdateSaleStatus completed. SaleId='{SaleId}'", saleId);
        return _mapper.Map<SaleResponseDTO>(sale);
    }

    // Método auxiliar privado para montar o evento de Pagamento
    private async Task PublishSalePaidEvent(Sale sale)
    {
        var eventMessage = new SalePaidEvent
        {
            SaleId = sale.Id,
            CorrelationId = Guid.NewGuid(),
            Items = sale.Items.Select(i => new SaleItemMessage(i.ProductId, i.Quantity)).ToList()
        };

        await _publishEndpoint.Publish(eventMessage);
        _logger.LogInformation(">>> The SalePaidEvent event is queued in Outbox. SaleId='{SaleId}'", sale.Id);
    }
}
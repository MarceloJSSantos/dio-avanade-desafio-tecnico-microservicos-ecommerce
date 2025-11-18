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
    private readonly ILogger<SaleService> _logger;

    public SaleService(
        ISaleRepository saleRepository,
        IStockManagerClient stockManagerClient,
        IMapper mapper,
        ILogger<SaleService> logger)
    {
        _saleRepository = saleRepository;
        _stockManagerClient = stockManagerClient;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<SaleResponseDTO> CreateSaleAsync(CreateSaleRequestDTO request)
    {
        _logger.LogInformation(">>> Creating sale for CustomerId='{CustomerId}' ItemsCount='{ItemsCount}'", request.CustomerId, request.Items?.Count ?? 0);
        var sale = new Sale(request.CustomerId);

        foreach (var itemRequest in request.Items!)
        {
            var productInfo = await _stockManagerClient.GetProductStockAsync(itemRequest.ProductId);

            if (productInfo == null)
            {
                _logger.LogWarning(">>> Product not found while creating sale. ProductId='{ProductId}'", itemRequest.ProductId);
                throw new KeyNotFoundException($"Produto com Id '{itemRequest.ProductId}' não encontrado.");
            }

            await ValidateItems(productInfo, itemRequest);

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
                    await SetStatusToPaid(sale);
                    break;
                case SaleStatus.Shipped:
                    await SetStatusToPaid(sale);
                    sale.SetStatusToShipped();
                    break;
                default:
                    _logger.LogWarning(">>> Invalid initial status requested: {Status}", requested);
                    throw new InvalidOperationException($"Status '{requested}' como inicial é inválido.");
            }
        }

        await _saleRepository.AddAsync(sale);
        await _saleRepository.UpdateAsync(sale);

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

        // Compensação: devolver itens ao estoque (chamadas externas)
        if (sale.Status != SaleStatus.PendingPayment)
        {
            foreach (var item in sale.Items)
            {
                _logger.LogInformation(">>> Returning stock for ProductId='{ProductId}' Quantity='{Quantity}' as part of cancel SaleId='{SaleId}'", item.ProductId, item.Quantity, saleId);
                var success = await _stockManagerClient.IncreaseStockAsync(item.ProductId, item.Quantity);
                if (!success)
                {
                    _logger.LogError(">>> Failed to return stock for ProductId='{ProductId}' during cancel SaleId='{SaleId}'", item.ProductId, saleId);
                    throw new InvalidOperationException($"Falha ao devolver estoque para o produto '{item.ProductId}'. Cancelamento abortado.");
                }
            }
        }

        sale.SetStatusToCancel();
        await _saleRepository.UpdateAsync(sale);

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
        _logger.LogInformation(">>> UpdateSaleStatus requested. SaleId='{SaleId}' RequestedStatus='{RequestedStatus}'", saleId, request.NewStatus);
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
                await SetStatusToPaid(sale);
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

        _logger.LogInformation(">>> UpdateSaleStatus completed. SaleId='{SaleId}' NewStatus='{NewStatus}'", saleId, newStatus);
        return _mapper.Map<SaleResponseDTO>(sale);
    }

    private async Task SetStatusToPaid(Sale sale)
    {
        _logger.LogInformation(">>> Setting sale to Paid. SaleId='{SaleId}'", sale.Id);
        sale.SetStatusToPaid();

        // Abate estoque: tratar falhas como erro de negócio
        foreach (var item in sale.Items)
        {
            _logger.LogInformation(">>> Decreasing stock for ProductId='{ProductId}' Quantity='{Quantity}' for SaleId='{SaleId}'", item.ProductId, item.Quantity, sale.Id);
            var success = await _stockManagerClient.DecreaseStockAsync(item.ProductId, item.Quantity);
            if (!success)
            {
                // Reverter estado de domínio se necessário ou lançar erro de negócio
                throw new InvalidOperationException($"Falha ao abater estoque para o produto '{item.ProductId}'. Operação abortada.");
            }
            _logger.LogDebug(">>> Decreased stock succeeded for ProductId='{ProductId}' SaleId='{SaleId}'", item.ProductId, sale.Id);
        }
    }

    private Task ValidateItems(ProductStockInfoDTO productInfo, CreateSaleItemRequestDTO itemRequest)
    {
        _logger.LogDebug(">>> Validating item ProductId='{ProductId}' Quantity='{Quantity}' Stock='{Stock}'", itemRequest.ProductId, itemRequest.Quantity, productInfo.StockQuantity);

        if (itemRequest.Quantity < 0)
        {
            _logger.LogWarning(">>> Invalid quantity for ProductId='{ProductId}': {Quantity}", itemRequest.ProductId, itemRequest.Quantity);
            throw new InvalidOperationException($"A quantidade (Item: '{itemRequest.ProductId}', quantidade: '{itemRequest.Quantity}') não pode ser menor que zero.");
        }

        if (productInfo.StockQuantity < itemRequest.Quantity)
        {
            _logger.LogWarning(">>> Insufficient stock for ProductId='{ProductId}' Requested={Requested} Available={Available}", itemRequest.ProductId, itemRequest.Quantity, productInfo.StockQuantity);
            throw new InvalidOperationException($"Estoque insuficiente para '{productInfo.ProductName}'.");
        }

        return Task.CompletedTask;
    }
}
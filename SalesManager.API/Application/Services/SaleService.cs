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
            var productInfo = await _stockManagerClient.GetProductStockAsync(itemRequest.ProductId);

            if (productInfo == null)
                throw new KeyNotFoundException($"Produto com Id '{itemRequest.ProductId}' não encontrado.");

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
            throw new KeyNotFoundException($"Venda com Id '{id}' não encontrada.");
        }
        return _mapper.Map<SaleResponseDTO>(sale);
    }

    public async Task<bool> CancelSaleAsync(int saleId)
    {
        var sale = await _saleRepository.GetByIdAsync(saleId);

        if (sale == null)
        {
            throw new KeyNotFoundException($"Venda com Id '{saleId}' não encontrada.");
        }

        if (sale.Status == SaleStatus.Shipped || sale.Status == SaleStatus.Completed || sale.Status == SaleStatus.Cancelled)
        {
            throw new InvalidOperationException($"A Venda com Id '{saleId}' está com o status '{sale.Status}', o qual não permite cancelamento.");
        }

        // Compensação: devolver itens ao estoque (chamadas externas)
        if (sale.Status != SaleStatus.PendingPayment)
        {
            foreach (var item in sale.Items)
            {
                var success = await _stockManagerClient.IncreaseStockAsync(item.ProductId, item.Quantity);
                if (!success)
                {
                    // falha na compensação => tratar como erro de negócio (InvalidOperationException)
                    throw new InvalidOperationException($"Falha ao devolver estoque para o produto '{item.ProductId}'. Cancelamento abortado.");
                }
            }
        }

        sale.SetStatusToCancel();
        await _saleRepository.UpdateAsync(sale);

        return true;
    }

    public async Task<PagedResult<SaleResponseDTO>> GetSalesAsync(int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var pagedSalesEntity = await _saleRepository.GetSalesAsync(pageNumber, pageSize);

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
            throw new KeyNotFoundException($"Venda com Id '{saleId}' não encontrada.");
        }

        var currentStatus = sale.Status;
        var newStatus = request.NewStatus;

        if (newStatus == SaleStatus.Cancelled)
        {
            throw new InvalidOperationException($"Transição inválida: Não é permitido mudar para '{SaleStatus.Cancelled}' por esta rota. Use a rota adequada.");
        }

        if ((int?)newStatus <= (int)currentStatus)
        {
            throw new InvalidOperationException($"Transição inválida: Não é permitido mudar de '{currentStatus}' para '{newStatus}'.");
        }

        switch (newStatus)
        {
            case SaleStatus.Paid:
                if (currentStatus != SaleStatus.PendingPayment)
                {
                    throw new InvalidOperationException($"Venda de Id '{saleId}' e com status '{currentStatus}' só pode ser marcada como '{newStatus}' se estiver como '{SaleStatus.PendingPayment}'.");
                }
                await SetStatusToPaid(sale);
                break;

            case SaleStatus.Shipped:
                if (currentStatus != SaleStatus.Paid)
                {
                    throw new InvalidOperationException($"Venda de Id '{saleId}' e com status '{currentStatus}' só pode ser marcada como '{newStatus}' se estiver como '{SaleStatus.Paid}'.");
                }
                sale.SetStatusToShipped();
                break;

            case SaleStatus.Completed:
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

        await _saleRepository.UpdateAsync(sale);

        return _mapper.Map<SaleResponseDTO>(sale);
    }

    private async Task SetStatusToPaid(Sale sale)
    {
        sale.SetStatusToPaid();

        // Abate estoque: tratar falhas como erro de negócio
        foreach (var item in sale.Items)
        {
            var success = await _stockManagerClient.DecreaseStockAsync(item.ProductId, item.Quantity);
            if (!success)
            {
                // Reverter estado de domínio se necessário ou lançar erro de negócio
                throw new InvalidOperationException($"Falha ao abater estoque para o produto '{item.ProductId}'. Operação abortada.");
            }
        }
    }

    private Task ValidateItems(ProductStockInfoDTO productInfo, CreateSaleItemRequestDTO itemRequest)
    {
        if (itemRequest.Quantity < 0)
            throw new InvalidOperationException($"A quantidade (Item: '{itemRequest.ProductId}', quantidade: '{itemRequest.Quantity}') não pode ser menor que zero.");

        if (productInfo.StockQuantity < itemRequest.Quantity)
            throw new InvalidOperationException($"Estoque insuficiente para '{productInfo.ProductName}'.");

        return Task.CompletedTask;
    }
}
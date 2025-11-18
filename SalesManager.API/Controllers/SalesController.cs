using Microsoft.AspNetCore.Mvc;
using SalesManager.API.Application.DTOs;
using SalesManager.API.Application.Interfaces;
using SalesManager.API.Application.Shared;

namespace SalesManager.API.Controllers
{
    [ApiController]
    [Route("api/sales")]
    public class SalesController : ControllerBase
    {
        private readonly ISaleService _saleService;
        private readonly ILogger<SalesController> _logger;

        public SalesController(ISaleService saleService, ILogger<SalesController> logger)
        {
            _saleService = saleService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSaleRequestDTO request)
        {
            _logger.LogInformation(">>> Create sale request for CustomerId={CustomerId}", request.CustomerId);
            if (!ModelState.IsValid)
            {
                _logger.LogWarning(">>> Create sale request invalid ModelState for CustomerId={CustomerId}", request.CustomerId);
                return ValidationProblem(ModelState);
            }

            var created = await _saleService.CreateSaleAsync(request);

            _logger.LogInformation(">>> Sale created. SaleId={SaleId} CustomerId={CustomerId}", created.Id, request.CustomerId);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation(">>> Get sale by id {SaleId} requested", id);
            var sale = await _saleService.GetSaleByIdAsync(id);

            _logger.LogDebug(">>> GetById result: SaleId={SaleId} Items={ItemsCount} Total={Total}", sale.Id, sale.Items?.Count ?? 0, sale.TotalPrice);
            return Ok(sale);
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            _logger.LogInformation(">>> Cancel sale requested. SaleId={SaleId}", id);
            var success = await _saleService.CancelSaleAsync(id);
            if (success)
            {
                _logger.LogInformation(">>> Sale cancelled successfully. SaleId={SaleId}", id);
                return NoContent();
            }

            _logger.LogError(">>> CancelSale returned false unexpectedly. SaleId={SaleId}", id);
            return StatusCode(500);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateSaleStatusRequestDTO request)
        {
            _logger.LogInformation(">>> UpdateSaleStatus requested. SaleId={SaleId} RequestedStatus={RequestedStatus}", id, request.NewStatus);
            if (!ModelState.IsValid)
            {
                _logger.LogWarning(">>> UpdateSaleStatus invalid ModelState. SaleId={SaleId}", id);
                return ValidationProblem(ModelState);
            }

            var updated = await _saleService.UpdateSaleStatusAsync(id, request);

            _logger.LogInformation(">>> UpdateSaleStatus completed. SaleId={SaleId} NewStatus={NewStatus}", id, request.NewStatus);
            return Ok(updated);
        }

        [HttpGet]
        public async Task<IActionResult> GetSales(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            _logger.LogInformation(">>> GetSales request received. PageNumber={PageNumber} PageSize={PageSize}", pageNumber, pageSize);

            var pagedResult = await _saleService.GetSalesAsync(pageNumber, pageSize);

            if (pagedResult == null || !pagedResult.Items.Any())
            {
                _logger.LogWarning(">>> GetSales returned no items for PageNumber={PageNumber}", pageNumber);
                throw new KeyNotFoundException($"Nenhuma venda encontrada na página solicitada '{pageNumber}'.");
            }

            _logger.LogInformation(">>> GetSales returned {Count} items", pagedResult.Items.Count());
            var response = new PagedResponse<SaleResponseDTO>(
                pagedResult.Items,
                pageNumber,
                pageSize,
                pagedResult.TotalCount
            );

            return Ok(response);
        }
    }
}
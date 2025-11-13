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

        public SalesController(ISaleService saleService)
        {
            _saleService = saleService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSaleRequestDTO request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var created = await _saleService.CreateSaleAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var sale = await _saleService.GetSaleByIdAsync(id);
            return Ok(sale);
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var success = await _saleService.CancelSaleAsync(id);
            if (success) return NoContent();
            return StatusCode(500);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateSaleStatusRequestDTO request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var updated = await _saleService.UpdateSaleStatusAsync(id, request);
            return Ok(updated);
        }

        [HttpGet]
        public async Task<IActionResult> GetSales(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {

            var pagedResult = await _saleService.GetSalesAsync(pageNumber, pageSize);

            if (pagedResult == null || !pagedResult.Items.Any())
            {
                throw new KeyNotFoundException($"Nenhuma venda encontrada na página  solicitada '{pageNumber}'.");
            }

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
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var sale = await _saleService.GetSaleByIdAsync(id);
                return Ok(sale);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateSale([FromBody] CreateSaleRequestDTO request)
        {
            try
            {
                if (request == null || request.Items == null || !request.Items.Any())
                    return BadRequest("Payload inválido ou sem itens.");

                var saleResponse = await _saleService.CreateSaleAsync(request);

                return CreatedAtAction(nameof(GetById), new { id = saleResponse.Id }, saleResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelSale(int id)
        {
            try
            {
                var success = await _saleService.CancelSaleAsync(id);
                if (!success) return BadRequest("Não foi possível cancelar a venda.");
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }

        [HttpGet]
        public async Task<IActionResult> GetSales(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var pagedResult = await _saleService.GetSalesAsync(pageNumber, pageSize);

                if (pagedResult == null || !pagedResult.Items.Any())
                {
                    return NotFound($"Nenhuma venda encontrada na página {pageNumber} solicitada.");
                }

                var response = new PagedResponse<SaleResponseDTO>(
                    pagedResult.Items,
                    pageNumber,
                    pageSize,
                    pagedResult.TotalCount
                );

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ocorreu um erro interno", details = ex.Message });
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateSaleStatus(
            [FromRoute] int id,
            [FromBody] UpdateSaleStatusRequestDTO request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var saleResponse = await _saleService.UpdateSaleStatusAsync(id, request);

                return Ok(saleResponse);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ocorreu um erro ao atualizar o status: " + ex.Message });
            }
        }
    }
}
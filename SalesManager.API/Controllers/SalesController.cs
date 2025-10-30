using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SalesManager.API.Application.DTOs;
using SalesManager.API.Application.Interfaces;

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
            var sale = await _saleService.GetSaleByIdAsync(id);
            if (sale == null) return NotFound();
            return Ok(sale);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSale([FromBody] CreateSaleRequestDTO request)
        {
            try
            {
                var saleResponse = await _saleService.CreateSaleAsync(request);
                // Retorna 201 Created com a localização do novo recurso
                return CreatedAtAction(nameof(GetById), new { id = saleResponse.Id }, saleResponse);
            }
            catch (Exception ex)
            {
                // Ex: Estoque insuficiente
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelSale(int id)
        {
            var success = await _saleService.CancelSaleAsync(id);
            if (!success) return BadRequest("Não foi possível cancelar a venda.");
            return NoContent();
        }

        /// <summary>
        /// GET api/sales?pageNumber=1&pageSize=20
        /// Retorna uma lista paginada de vendas.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSales(
            [FromQuery] int pageNumber = 1, // Valor padrão 1
            [FromQuery] int pageSize = 20)  // Valor padrão 20
        {
            var sales = await _saleService.GetSalesAsync(pageNumber, pageSize);

            if (sales == null || !sales.Any())
            {
                return NotFound("Nenhuma venda encontrada na página solicitada.");
            }

            // Em um sistema real, você retornaria aqui metadados de paginação (Total de páginas, Total de itens, etc.)
            return Ok(sales);
        }
    }
}
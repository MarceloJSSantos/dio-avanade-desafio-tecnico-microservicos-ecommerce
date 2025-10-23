using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StockManager.API.Domain.DTOs;
using StockManager.API.Domain.Interfaces;
using StockManager.API.Domain.Entities;

namespace StockManager.API.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IMapper _mapper;

        public ProductController(IProductService productService, IMapper mapper)
        {
            _productService = productService;
            _mapper = mapper;
        }

        [HttpPost]
        public async Task<IActionResult> Create(ProductDTO productDTO)
        {
            // 1. Uso do AutoMapper para converter DTO -> Entidade
            var product = _mapper.Map<Product>(productDTO);

            // 2. Chamada ao Service (que lida apenas com a Entidade)
            var productCreated = await _productService.CreateProductAsync(product);

            // CORREÇÃO AQUI: Converter a Entity de volta para o DTO de Resposta
            var responseDto = _mapper.Map<ProductResponseDTO>(productCreated);

            // return CreatedAtAction(nameof(GetById), new { id = productCreated.ProductId }, productCreated);
            return CreatedAtAction(nameof(GetById), new { id = productCreated.ProductId }, responseDto);
        }

        // GET: Consulta todos os produtos
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // 1. Chamada ao Service (Assíncrona)
            var products = await _productService.GetAllProductsAsync();

            // CORREÇÃO AQUI: Converter a Coleção de Entities para a Coleção de DTOs
            var responseDtos = _mapper.Map<IEnumerable<ProductResponseDTO>>(products);

            // HTTP 200 Ok
            return Ok(responseDtos);
        }

        // GET: Consulta produto por ID (Método auxiliar)
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);

            if (product == null)
            {
                return NotFound($"Produto com ID {id} não encontrado."); // HTTP 404
            }

            // CORREÇÃO AQUI: Converter a Entity para o DTO de Resposta
            var responseDto = _mapper.Map<ProductResponseDTO>(product);

            return Ok(responseDto);
        }

        // PATCH: Atualiza a quantidade em estoque (delta)
        [HttpPatch("{productId}/stock")]
        public async Task<IActionResult> UpdateStock(int productId, [FromBody] UpdateStockDTO updateStock)
        {
            try
            {
                // Chamada ao Service com a lógica de mudança (delta)
                var newStock = await _productService.UpdateStockAsync(
                    productId,
                    updateStock.TransactionAmount
                );

                // CORREÇÃO: Cria o DTO de Resposta manualmente
                var responseDto = new UpdateStockResponseDTO
                {
                    ProductId = productId, // O ID vem do parâmetro da rota
                    TransactionAmount = newStock    // O saldo vem do retorno do Service (int)
                };

                // HTTP 200 Ok com o novo saldo
                //return Ok(new { ProductId = productId, NewStock = newStock });
                return Ok(responseDto);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message); // Retorna HTTP 404
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message); // Retorna HTTP 400 (ex: falta de estoque)
            }
            catch (Exception ex)
            {
                // Captura outros erros de transação ou BD
                return StatusCode(500, $"Erro interno ao processar a atualização de estoque.\nOutro erro: {ex.Message}");
            }
        }
    }
}
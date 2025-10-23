using AutoMapper;
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
            var product = _mapper.Map<Product>(productDTO);
            var productCreated = await _productService.CreateProductAsync(product);

            var responseDto = _mapper.Map<ProductResponseDTO>(productCreated);

            return CreatedAtAction(nameof(GetById), new { id = productCreated.ProductId }, responseDto);
        }


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllProductsAsync();

            var responseDtos = _mapper.Map<IEnumerable<ProductResponseDTO>>(products);

            return Ok(responseDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);

            if (product == null)
            {
                return NotFound($"Produto com ID {id} não encontrado.");
            }

            var responseDto = _mapper.Map<ProductResponseDTO>(product);

            return Ok(responseDto);
        }

        [HttpPatch("{productId}/stock")]
        public async Task<IActionResult> UpdateStock(int productId, [FromBody] UpdateStockDTO updateStock)
        {
            try
            {
                var newStock = await _productService.UpdateStockAsync(
                    productId,
                    updateStock.TransactionAmount
                );

                var responseDto = new UpdateStockResponseDTO
                {
                    ProductId = productId,
                    TransactionAmount = newStock
                };

                return Ok(responseDto);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro interno ao processar a atualização de estoque.\nOutro erro: {ex.Message}");
            }
        }
    }
}
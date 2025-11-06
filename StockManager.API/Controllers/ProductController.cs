using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using StockManager.API.Application.DTOs;
using StockManager.API.Application.Interfaces;
using StockManager.API.Domain.Entities;
using SalesManager.API.Application.Shared;

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
        public async Task<IActionResult> GetPagedProductsAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var paged = await _productService.GetPagedProductsAsync(page, pageSize);

            if (paged == null || !paged.Items.Any())
            {
                throw new KeyNotFoundException($"Nenhum produto encontrado na página {page} solicitada.");
            }

            var response = new PagedResponse<ProductResponseDTO>(
                paged.Items,
                page,
                pageSize,
                paged.TotalCount
            );
            ;

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);

            if (product == null)
            {
                throw new KeyNotFoundException($"Produto com ID {id} não encontrado.");
            }

            var responseDto = _mapper.Map<ProductResponseDTO>(product);

            return Ok(responseDto);
        }

        [HttpPatch("{productId}/stock")]
        public async Task<IActionResult> UpdateStock(int productId, [FromBody] UpdateStockDTO updateStock)
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
    }
}
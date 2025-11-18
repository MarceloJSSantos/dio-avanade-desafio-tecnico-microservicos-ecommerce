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
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService productService, IMapper mapper, ILogger<ProductController> logger)
        {
            _productService = productService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create(ProductDTO productDTO)
        {
            _logger.LogInformation(">>> Create product request received: '{ProductName}'", productDTO?.Name);
            var product = _mapper.Map<Product>(productDTO);
            var productCreated = await _productService.CreateProductAsync(product);

            _logger.LogInformation(">>> Product created successfully. ProductId='{ProductId}'", productCreated.ProductId);
            var responseDto = _mapper.Map<ProductResponseDTO>(productCreated);

            return CreatedAtAction(nameof(GetById), new { id = productCreated.ProductId }, responseDto);
        }

        [HttpGet]
        public async Task<IActionResult> GetPagedProductsAsync([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            _logger.LogInformation(">>> GetProducts paged request: page='{Page}' pageSize='{PageSize}'", page, pageSize);
            var paged = await _productService.GetPagedProductsAsync(page, pageSize);

            if (paged == null || !paged.Items.Any())
            {
                throw new KeyNotFoundException($"Nenhum produto encontrado na página {page} solicitada.");
            }

            _logger.LogInformation(">>> GetPagedProductsAsync returned {Count} items", paged.Items.Count());
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
            _logger.LogInformation(">>> GetById for ProductId='{ProductId}'", id);
            var product = await _productService.GetProductByIdAsync(id);

            if (product == null)
            {
                _logger.LogWarning(">>> Product not found. ProductId='{ProductId}'", id);
                throw new KeyNotFoundException($"Produto com ID {id} não encontrado.");
            }

            var responseDto = _mapper.Map<ProductResponseDTO>(product);

            return Ok(responseDto);
        }

        [HttpPatch("{productId}/stock")]
        public async Task<IActionResult> UpdateStock(int productId, [FromBody] UpdateStockDTO updateStock)
        {
            _logger.LogInformation(">>> UpdateStock request: ProductId='{ProductId}' TransactionAmount='{Tx}'", productId, updateStock?.TransactionAmount);
            var newStock = await _productService.UpdateStockAsync(
                productId,
                (int)updateStock.TransactionAmount
            );

            var responseDto = new UpdateStockResponseDTO(
                productId,
                newStock
            );

            _logger.LogInformation(">>> Stock updated: ProductId='{ProductId}' NewStock='{NewStock}'", productId, newStock);
            return Ok(responseDto);
        }
    }
}
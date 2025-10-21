using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StockManager.API.Domain.DTOs;

namespace StockManager.API.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : ControllerBase
    {
        [HttpPost]
        public IActionResult Create(ProductDTO productDTO)
        {
            return Ok("Produto Criado");
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok("Lista de produtos");
        }

        [HttpPatch("{productId}/stock")]
        public IActionResult UpdateStock(int productId, UpdateStockDTO updateStock)
        {
            return Ok("Atualização do estoque");
        }
    }
}
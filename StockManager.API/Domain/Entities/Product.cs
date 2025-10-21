using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StockManager.API.Domain.Entities
{
    public class Product
    {
        // Chave Primária
        public int ProductID { get; set; }

        // Dados do Produto
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }

        // Dados do Estoque (Quantidade)
        public int QuantityInStock { get; set; }
    }
}
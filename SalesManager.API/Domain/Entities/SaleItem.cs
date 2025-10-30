using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesManager.API.Domain.Entities
{
    public class SaleItem
    {
        public int Id { get; private set; } // <-- Mudança: int
        public int SaleId { get; private set; } // <-- Mudança: int
        public int ProductId { get; private set; } // <-- Mudança: int
        public int Quantity { get; private set; }
        public decimal UnitPrice { get; private set; }
        public decimal Subtotal => Quantity * UnitPrice;

        // Construtor
        public SaleItem(int saleId, int productId, int quantity, decimal unitPrice) // <-- Mudança: int
        {
            // O 'Id' também será gerado pelo banco
            SaleId = saleId;
            ProductId = productId;
            Quantity = quantity;
            UnitPrice = unitPrice;
        }
    }
}
namespace SalesManager.API.Domain.Entities
{
    public class SaleItem
    {
        public int Id { get; private set; }
        public int SaleId { get; private set; }
        public int ProductId { get; private set; }
        public int Quantity { get; private set; }
        public decimal UnitPrice { get; private set; }
        public decimal Subtotal => Quantity * UnitPrice;

        public SaleItem(int saleId, int productId, int quantity, decimal unitPrice)
        {
            SaleId = saleId;
            ProductId = productId;
            Quantity = quantity;
            UnitPrice = unitPrice;
        }
    }
}
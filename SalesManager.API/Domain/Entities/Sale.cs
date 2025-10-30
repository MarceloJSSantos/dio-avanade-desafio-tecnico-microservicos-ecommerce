using SalesManager.API.Domain.Enums;

namespace SalesManager.API.Domain.Entities
{
    public class Sale
    {
        public int Id { get; private set; } // <-- Mudança: int
        public int CustomerId { get; private set; } // <-- Mudança: int
        public DateTime CreatedAt { get; private set; }
        public SaleStatus Status { get; private set; }
        public decimal TotalPrice { get; private set; }

        private readonly List<SaleItem> _items = new List<SaleItem>();
        public IReadOnlyCollection<SaleItem> Items => _items.AsReadOnly();

        // Construtor
        public Sale(int customerId) // <-- Mudança: int
        {
            // O 'Id' agora será gerado pelo banco de dados (Auto-increment)
            CustomerId = customerId;
            CreatedAt = DateTime.UtcNow;
            Status = SaleStatus.PendingPayment;
        }

        public void AddItem(int productId, int quantity, decimal unitPrice) // <-- Mudança: int
        {
            if (Status != SaleStatus.PendingPayment)
            {
                throw new InvalidOperationException("Não é possível adicionar itens a uma venda que não está pendente.");
            }

            var newItem = new SaleItem(this.Id, productId, quantity, unitPrice);
            _items.Add(newItem);
            CalculateTotalPrice();
        }

        // ... (Restante dos métodos: CalculateTotalPrice, SetStatusToPaid, Cancel) ...
        public void CalculateTotalPrice()
        {
            TotalPrice = _items.Sum(item => item.Subtotal);
        }

        public void SetStatusToPaid()
        {
            // Regra de negócio: só pode pagar se estiver pendente
            if (Status == SaleStatus.PendingPayment)
            {
                Status = SaleStatus.Paid;
            }
        }

        public void Cancel()
        {
            // Regra de negócio: só pode cancelar se não estiver enviado/concluído
            if (Status != SaleStatus.Shipped && Status != SaleStatus.Completed)
            {
                Status = SaleStatus.Cancelled;
            }
        }
    }
}
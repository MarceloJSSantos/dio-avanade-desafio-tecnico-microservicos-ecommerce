using SalesManager.API.Domain.Enums;

namespace SalesManager.API.Domain.Entities
{
    public class Sale
    {
        public int Id { get; private set; }
        public int CustomerId { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public SaleStatus Status { get; private set; }
        public decimal TotalPrice { get; private set; }

        private readonly List<SaleItem> _items = new List<SaleItem>();
        public IReadOnlyCollection<SaleItem> Items => _items.AsReadOnly();

        public Sale(int customerId)
        {
            CustomerId = customerId;
            CreatedAt = DateTime.UtcNow;
            Status = SaleStatus.PendingPayment;
        }

        public void AddItem(int productId, int quantity, decimal unitPrice)
        {
            if (Status != SaleStatus.PendingPayment)
            {
                throw new InvalidOperationException("Não é possível adicionar itens a uma venda que não está pendente.");
            }

            var newItem = new SaleItem(this.Id, productId, quantity, unitPrice);
            _items.Add(newItem);
            CalculateTotalPrice();
        }

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

        public void SetStatusToShipped()
        {
            // Regra de negócio: só pode Enviar se estiver Pago
            if (Status == SaleStatus.Paid)
            {
                Status = SaleStatus.Shipped;
            }
            else
            {
                throw new InvalidOperationException("Só é possível enviar uma venda que esteja com status 'Paid'.");
            }
        }

        public void SetStatusToCancel()
        {
            // Regra de negócio: só pode cancelar se não estiver enviado/concluído
            if (Status != SaleStatus.Shipped && Status != SaleStatus.Completed && Status != SaleStatus.Cancelled)
            {
                Status = SaleStatus.Cancelled;
            }
            else
            {
                throw new InvalidOperationException("Não é possível cancelar uma venda já enviada/concluída ou já cancelada.");
            }
        }

        public void SetStatusToCompleted()
        {
            // Regra de negócio: só pode completar se estiver enviado
            if (Status == SaleStatus.Shipped)
            {
                Status = SaleStatus.Completed;
            }
            else
            {
                throw new InvalidOperationException("Só é possível concluir uma venda que esteja com status 'Shipped'.");
            }
        }
    }
}
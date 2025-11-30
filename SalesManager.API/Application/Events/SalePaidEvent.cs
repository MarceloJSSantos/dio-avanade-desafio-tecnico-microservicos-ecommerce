namespace SalesManager.API.Application.Events
{
    public record SalePaidEvent
    {
        public int SaleId { get; init; }
        public Guid CorrelationId { get; init; } = Guid.NewGuid();
        public List<SaleItemMessage> Items { get; init; } = new();
    }

    public record SaleItemMessage(int ProductId, int Quantity);
}
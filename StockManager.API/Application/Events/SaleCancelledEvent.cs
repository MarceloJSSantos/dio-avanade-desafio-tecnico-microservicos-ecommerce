namespace SalesManager.API.Application.Events
{
    public record SaleCancelledEvent
    {
        public int SaleId { get; init; }
        public DateTime CancelledAt { get; init; }
        public List<SaleItemMessage> Items { get; init; } = new();
    }
}
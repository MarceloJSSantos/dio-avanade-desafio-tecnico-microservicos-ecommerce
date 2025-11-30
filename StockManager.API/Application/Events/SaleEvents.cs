namespace SalesManager.API.Application.Events
{
    public record SaleItemMessage(int ProductId, int Quantity);
    public record SalePaidEvent(int SaleId, List<SaleItemMessage> Items);
    public record StockSuccessEvent(int SaleId, DateTime ProcessedAt);
    public record StockErrorEvent(int SaleId, string Reason);
}
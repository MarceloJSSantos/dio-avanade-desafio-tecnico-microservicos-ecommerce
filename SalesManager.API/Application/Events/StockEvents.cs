namespace SalesManager.API.Application.Events
{
    // Eventos que o Estoque responde (Feedback)
    public record StockSuccessEvent(int SaleId, DateTime ProcessedAt);
    public record StockErrorEvent(int SaleId, string Reason);
}
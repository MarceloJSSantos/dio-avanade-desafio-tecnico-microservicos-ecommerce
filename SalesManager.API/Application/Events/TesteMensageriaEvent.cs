namespace SalesManager.API.Application.Events
{
    // Um evento simples só para ver se chega lá
    public record TesteMensageriaEvent
    {
        public string Mensagem { get; init; } = string.Empty;
        public DateTime DataEnvio { get; init; } = DateTime.UtcNow;
    }
}
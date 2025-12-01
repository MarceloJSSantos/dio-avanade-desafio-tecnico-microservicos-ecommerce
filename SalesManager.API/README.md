# SalesManager.API

Descrição

- Responsabilidade: gerenciamento de vendas/pedidos, aplicação de regras de negócio de transição de status e coordenação com o StockManager para reservar/abatir estoque.
  - Criar / consultar / cancelar vendas
  - Atualizar status (PendingPayment → Paid → Shipped → Completed / Cancelled)
  - Publicar eventos de domínio via RabbitMQ (SalePaid, SaleCancelled, etc.)
  - Consumir respostas do estoque (StockResponse/StockError)

Tecnologias / Bibliotecas

- .NET 9, C#
- EF Core (SQL Server)
- AutoMapper
- MassTransit (RabbitMQ)
- Polly (HttpClient resilience)
- Swashbuckle (Swagger)
- ILogger + CorrelationIdMiddleware

Configuração (principais variáveis de ambiente)

- ConnectionStrings\_\_DbConnection — connection string do banco
- StockManagerUrl — URL base do StockManager (ex.: http://stockmanager:80)
- RabbitMQ**Host, RabbitMQ**User, RabbitMQ\_\_Password
- ASPNETCORE_ENVIRONMENT

Principais endpoints (documentação completa em Swagger)

- POST /api/sales
  - Body exemplo:
    {
    "customerId": 1,
    "items": [
    { "productId": 10, "quantity": 2, "unitPrice": 99.9 }
    ],
    "initialStatus": "PendingPayment" // opcional, aceita enum por nome
    }
- GET /api/sales?pageNumber=1&pageSize=20
- GET /api/sales/{id}
- PATCH /api/sales/{id}/status
  - Body exemplo: { "status": "Paid" }
- PUT /api/sales/{id}/cancel
- Health: /health/live, /health/ready
- Swagger UI: /swagger

Eventos (assíncrono)

- Publica:
  - sale.created (PedidoCriado) — quando venda é criada (opcional para integração)
  - sale.paid — quando a venda é marcada como paga
  - sale.cancelled — quando a venda é cancelada
- Consome:
  - stock.response — confirmação de reserva/abatimento do estoque
  - stock.error — notificação de erro no estoque (compensação)

Exemplo de evento "sale.created"
{
"saleId": 123,
"customerId": 1,
"items": [
{ "productId": 10, "quantity": 2 }
],
"totalPrice": 199.8
}

Regras de domínio e erros

- Regras de transição implementadas na entidade Sale (métodos SetStatusToPaid, SetStatusToShipped, etc.)
- Em violações de regra lança InvalidOperationException (tratado pelo ExceptionMiddleware → 400)
- KeyNotFoundException → 404
- DbUpdateConcurrencyException → 409

Rodando localmente

- dotnet restore
- dotnet ef database update
- dotnet run --project SalesManager.API
- Ou via docker-compose (ver README raiz)

Observações operacionais

- HttpClient configurado com Polly para retry, circuit-breaker e timeout ao chamar StockManager.
- Logs estruturados (ILogger) em services/controllers; CorrelationId via header X-Correlation-Id.

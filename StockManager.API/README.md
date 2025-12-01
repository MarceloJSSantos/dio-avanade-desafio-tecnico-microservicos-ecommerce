# StockManager.API

Descrição

- Responsabilidade: administração de catálogo e estoque de produtos.
  - CRUD de produtos
  - Consulta disponibilidade
  - Ajustes de estoque (incremento / decremento)
  - Expõe endpoints HTTP e consome/publíca eventos via RabbitMQ

Tecnologias / Bibliotecas

- .NET 9, C#
- EF Core (SQL Server)
- AutoMapper
- MassTransit (RabbitMQ)
- Polly (resiliência, quando aplicável)
- Swashbuckle (Swagger)
- ILogger + CorrelationIdMiddleware

Configuração (principais variáveis de ambiente)

- ConnectionStrings\_\_DbConnection — connection string do banco (SQL Server)
- RabbitMQ\_\_Host — host do RabbitMQ (ex: rabbitmq)
- RabbitMQ\_\_User — usuário RabbitMQ
- RabbitMQ\_\_Password — senha RabbitMQ
- ASPNETCORE_ENVIRONMENT — Development/Production
- LOGGING (opcional via appsettings)

Principais endpoints (documentação completa em Swagger)

- GET /api/products
  - Suporta paginação: ?page=1&pageSize=20
- GET /api/products/{id}
- POST /api/products
- PATCH /api/products/{id}/stock
  - Body: { "transactionAmount": int }
- Health:**🚧 EM DESENVOLVIMENTO: Esta seção de documentação será adicionada em breve! 🚧**
  - GET /health/live
  - GET /health/ready
- Swagger UI: /swagger

Eventos (assíncrono)

- Consome:
  - sale.created (PedidoCriado) — solicita reservar/abater estoque
- Publica:
  - stock.response — confirmação de operação (sucesso)
    - exemplo payload:
      {
      "saleId": 123,
      "productId": 456,
      "success": true,
      "available": 10
      }
  - stock.error — falha no processamento (ex.: estoque insuficiente)
    - exemplo payload:
      {
      "saleId": 123,
      "productId": 456,
      "error": "Estoque insuficiente"
      }

Rodando localmente

- Dotnet:
  - dotnet restore
  - dotnet ef database update (caso use migrations)
  - dotnet run --project StockManager.API
- Docker:
  - Configure connection strings e rabbitmq no docker-compose e rode docker-compose up --build

Observações operacionais

- Validações com DataAnnotations nos DTOs.
- Logging estruturado (ILogger<T>) em controllers, services e HttpClient.
- Política de resiliência e timeout aplicadas ao consumir APIs externas (quando houver).

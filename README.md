# DESAFIO TÉCNICO AVANADE

Resumo arquitetural

- Projeto composto por 3 microsserviços independentes:
  - APIGateway — ponto de entrada/roteamento.
  - StockManager — gerencia produtos e estoque.
  - SalesManager — processa pedidos/vendas e coordena com estoque.
- Comunicação:
  - Sincrona: HTTP/REST (Swagger em cada serviço).
  - Assíncrona: RabbitMQ (eventos de domínio: e.g., PedidoCriado, StockResponse).
- Cross-cutting: health checks, middleware de exceção/CorrelationId, logs estruturados (ILogger), resiliência (Polly), EF Core.

Pré-requisitos globais

- .NET 9 SDK
- Docker & Docker Compose
  **🚧 EM DESENVOLVIMENTO: Esta seção de documentação será adicionada em breve! 🚧**
- RabbitMQ (poderá ser iniciado pelo docker-compose)
- BD SQLServer (poderá ser iniciado pelo docker-compose)
- dotnet-ef (opcional, para migrations)
- Postman / HTTP client (recomendado)

Leia também os README de cada microsserviço

- [StockManager](./StockManager.API/README.md)
- [SalesManager](./SalesManager.API/README.md)
- [APIGateway](./APIGateway/README.md)

Subir a stack (exemplo)

1. Na raiz do repositório:
   docker-compose up --build
2. URLs típicas:
   - API Gateway (Swagger):
     **🚧 EM DESENVOLVIMENTO: Esta seção de documentação será adicionada em breve! 🚧**
   - StockManager (Swagger): http://localhost:5101/swagger
   - SalesManager (Swagger): http://localhost:5102/swagger
   - RabbitMQ Management: http://localhost:15672 (guest/guest por padrão)

Observações

- Configure variáveis de ambiente (connection strings, RabbitMQ URL, etc.) antes de rodar.
- Cada serviço expõe health checks (/health/live e /health/ready). (Em desenvolvimento)
- Logs possuem header X-Correlation-Id para rastreabilidade.

> 🚧 **AVISO: Testes em Desenvolvimento**
>
> Testes unitários e de integração ainda serão implementados:
>
> - [ ] Testes Unitários (xUnit/NUnit)
> - [ ] Testes de Integração com Docker Compose
> - [ ] Testes E2E
>
> Versão atual focada em arquitetura e comunicação entre serviços.

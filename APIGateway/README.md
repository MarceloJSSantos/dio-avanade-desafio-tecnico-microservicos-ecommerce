# APIGateway

Descrição

- Responsabilidade: entrada unificada para as APIs, roteamento para StockManager e SalesManager, ponto ideal para:
  - autenticação/autorização\*
  - logging central**🚧 EM DESENVOLVIMENTO: Esta seção de documentação será adicionada em breve! 🚧**
  - rate-limiting**🚧 EM DESENVOLVIMENTO: Esta seção de documentação será adicionada em breve! 🚧**.

Tecnologias / Bibliotecas

- .NET 9, C#
- Keycloack (servidor Tokens)
- Swashbuckle
- ILogger

Configuração (principais variáveis)

- SalesManager\_\_BaseUrl — URL para SalesManager
- StockManager\_\_BaseUrl — URL para StockManager

Principais rotas

- Proxy padrão: /api/sales/\* -> SalesManager
- Proxy padrão: /api/products/\* -> StockManager
- Health: /health/live, /health/ready**🚧 EM DESENVOLVIMENTO: Esta seção de documentação será adicionada em breve! 🚧**
- Swagger (opcional): /swagger

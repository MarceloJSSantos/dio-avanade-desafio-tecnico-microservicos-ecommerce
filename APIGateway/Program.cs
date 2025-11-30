using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.Extensions.Configuration; // Necessário para AddJsonFile

var builder = WebApplication.CreateBuilder(args);

// 1. Configura o Ocelot para carregar o arquivo ocelot.json
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot();

// Adicione outros serviços, como o Swagger/OpenAPI, se desejar
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "API Gateway",
            Version = "v1",
            Description = "API Gateway: Ponto de entrada unificado e fachada para os microsserviços da aplicação (StockManager e SalesManager ). Gerencia roteamento, segurança (autenticação/autorização) e monitoramento de todas as requisições externas.",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "Marcelo JS Santos",
                Email = "marcelojssantos2012@gmail.com"
            }
        });
}
);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SAPI Gateway");
    });
}

app.UseHttpsRedirection();

// 2. Adiciona o middleware do Ocelot
app.MapControllers();
await app.UseOcelot(); // O 'await' é necessário para o UseOcelot

app.Run();
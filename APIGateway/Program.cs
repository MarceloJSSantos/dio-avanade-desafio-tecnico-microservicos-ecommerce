using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configuração para que o Ocelot use a pasta OcelotConfig (na raiz do projeto)
builder.Configuration.AddOcelot("OcelotConfig", builder.Environment);

// Configuração do Serviço de Autenticação
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        // valores que vem do Keycloack
        options.Authority = "http://localhost:8080/realms/DesafioTecnicoAvanade"; // Ex: URL do seu IdentityServer ou Provedor de JWT
        options.Audience = "api-gateway"; // Ex: Nome da sua Audience. Usei api-gateway, mas o Keycloak não envia
        options.RequireHttpsMetadata = false; // Mantenha 'true' em produção
    });

// Passa a configuração consolidada do Ocelot
builder.Services.AddOcelot(builder.Configuration);

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

// Adiciona o middleware do Ocelot
app.MapControllers();
// Tem que vir ANTES de app.UseOcelot()
app.UseAuthentication();
app.UseAuthorization();

await app.UseOcelot();

app.Run();
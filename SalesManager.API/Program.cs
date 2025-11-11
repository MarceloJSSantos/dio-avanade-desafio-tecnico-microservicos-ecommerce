using Microsoft.EntityFrameworkCore;
using SalesManager.API.Application.Interfaces;
using SalesManager.API.Domain.Repositories;
using SalesManager.API.Infrastructure.Db;
using SalesManager.API.Infrastructure.HttpClients;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var DbConnection = builder.Configuration.GetConnectionString("DbConnection");

// Add services to the container.

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SaleskManager API",
        Version = "v1",
        Description = "Microsserviço de Gestão de Vendas para a API",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Marcelo JS Santos",
            Email = "marcelojssantos2012@gmail.com"
        }
    });
}
);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Permite receber/retornar enums como nomes (ex: "Paid") no JSON
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Dados de entrada inválidos. Verifique os erros para mais detalhes."
            };
            return new BadRequestObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });

// Configurar DbContext (ex: SQL Server)
builder.Services.AddDbContext<SalesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnection")));

// Injeção de Dependência (Scoped, Transient, Singleton)
builder.Services.AddScoped<ISaleRepository, SaleRepository>();
builder.Services.AddScoped<ISaleService, SaleService>();

// Configurar o HttpClient para o StockManager
builder.Services.AddHttpClient<IStockManagerClient, StockManagerClient>();

// Registrar o AutoMapper
// Isso irá escanear o assembly que contém o 'MappingProfile' 
// e registrar todos os perfis de mapeamento encontrados.
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddOpenApi();

var app = builder.Build();

// Enable Swagger e o Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SalesManager API V1");
    });
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

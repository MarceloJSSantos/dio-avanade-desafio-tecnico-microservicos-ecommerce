using Microsoft.EntityFrameworkCore;
using SalesManager.API.Application.Interfaces;
using SalesManager.API.Domain.Repositories;
using SalesManager.API.Infrastructure.Db;
using SalesManager.API.Infrastructure.HttpClients;

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
});

builder.Services.AddControllers();

// Configurar DbContext (ex: SQL Server)
builder.Services.AddDbContext<SalesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnection")));

// Injeção de Dependência (Scoped, Transient, Singleton)
builder.Services.AddScoped<ISaleRepository, SaleRepository>();
builder.Services.AddScoped<ISaleService, SaleService>();

// Configurar o HttpClient para o StockManager
builder.Services.AddHttpClient<IStockManagerClient, StockManagerClient>();

// <-- Adicionado: Registrar o AutoMapper
// Isso irá escanear o assembly que contém o 'MappingProfile' 
// e registrar todos os perfis de mapeamento encontrados.
//builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
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

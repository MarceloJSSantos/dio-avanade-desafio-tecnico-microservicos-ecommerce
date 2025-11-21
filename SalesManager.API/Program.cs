using Microsoft.EntityFrameworkCore;
using SalesManager.API.Application.Interfaces;
using SalesManager.API.Domain.Repositories;
using SalesManager.API.Infrastructure.Db;
using SalesManager.API.Infrastructure.HttpClients;
using Microsoft.AspNetCore.Mvc;
using SalesManager.API.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

// configure structured logging providers (console + debug) and allow filtering via appsettings
builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

builder.Logging.AddConsole()
    .AddSimpleConsole(options =>
    {
        options.IncludeScopes = true;
        options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
        options.TimestampFormat = "[HH:mm:ss.fff] ";
        options.SingleLine = false;
    });

builder.Logging.AddDebug();
// Reduzir verbosidade de EF Core SQL em produção
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

var DbConnection = builder.Configuration.GetConnectionString("DbConnection");

var stockManagerUrl = builder.Configuration["ServiceUrls:StockManager"]
                      ?? throw new InvalidOperationException("URL do StockManager não configurada.");

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
            var problemDetails = new ProblemDetails
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = string.Join(" | ", context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage))
            };

            return new BadRequestObjectResult(problemDetails);
        };
    });

// Configurar DbContext (ex: SQL Server)
builder.Services.AddDbContext<SalesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnection")));

// Injeção de Dependência (Scoped, Transient, Singleton)
builder.Services.AddScoped<ISaleRepository, SaleRepository>();
builder.Services.AddScoped<ISaleService, SaleService>();

// Configurar o HttpClient para o StockManager
builder.Services.AddHttpClient<IStockManagerClient, StockManagerClient>(client =>
{
    client.BaseAddress = new Uri(stockManagerUrl);
    client.Timeout = TimeSpan.FromSeconds(10); // Timeout global da requisição
})
.AddStandardResilienceHandler(); // Adiciona Retry, Circuit Breaker, etc. automaticamente


// Registrar o AutoMapper
// Isso irá escanear o assembly que contém o 'MappingProfile' 
// e registrar todos os perfis de mapeamento encontrados.
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddOpenApi();

var app = builder.Build();

// registrar middleware de correlação e de exceção
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SalesManager.API.Infrastructure.Middleware.ExceptionMiddleware>();

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

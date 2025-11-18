using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockManager.API.Application.Interfaces;
using StockManager.API.Application.Services;
using StockManager.API.Infrastructure.Db;
using StockManager.API.Infrastructure.Middleware;

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

if (string.IsNullOrWhiteSpace(DbConnection))
{
    using var loggerFactory = LoggerFactory.Create(lb => lb.AddConsole());
    var logger = loggerFactory.CreateLogger("Startup");
    logger.LogCritical("Connection string 'DbConnection' não configurada.");
    throw new InvalidOperationException("Connection string 'DbConnection' não configurada. Use User Secrets ou appsettings.");
}

builder.Services.AddDbContext<StockManagerContext>(options =>
    options.UseSqlServer(DbConnection));

builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "StockManager API",
        Version = "v1",
        Description = "Microsserviço de Gestão de Estoque para a API",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Marcelo JS Santos",
            Email = "marcelojssantos2012@gmail.com"
        }
    });

});

builder.Services.AddControllers()
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

builder.Services.AddOpenApi();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

//middleware de correlação (gera X-Correlation-Id) antes do middleware de exceções
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "StockManager API V1");
    });
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
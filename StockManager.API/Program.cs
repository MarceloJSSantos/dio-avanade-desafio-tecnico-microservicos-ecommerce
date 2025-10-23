using Microsoft.EntityFrameworkCore;
using StockManager.API.Domain.Interfaces;
using StockManager.API.Domain.Services;
using StockManager.API.Infrastructure.Db;

var builder = WebApplication.CreateBuilder(args);
var DbConnection = builder.Configuration.GetConnectionString("DbConnection");

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

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

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
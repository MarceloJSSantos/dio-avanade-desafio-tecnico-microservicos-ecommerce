using Microsoft.EntityFrameworkCore;
using StockManager.API.Domain.Entities;

namespace StockManager.API.Infrastructure.Db
{
    public class StockManagerContext : DbContext
    {
        public StockManagerContext(DbContextOptions<StockManagerContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
    }
}
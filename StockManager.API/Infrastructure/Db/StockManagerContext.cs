using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockManager.API.Domain.Entities;

namespace StockManager.API.Infrastructure.Db
{
    public class StockManagerContext : DbContext
    {
        public StockManagerContext(DbContextOptions<StockManagerContext> options) : base(options)
        {
        }

        public DbSet<Product> products { get; set; }

    }
}
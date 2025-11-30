using Microsoft.EntityFrameworkCore;
using SalesManager.API.Domain.Entities;
using MassTransit;

namespace SalesManager.API.Infrastructure.Db
{
    public class SalesDbContext : DbContext
    {
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleItem> SaleItems { get; set; }

        public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Sale>(e =>
            {
                e.HasKey(s => s.Id);

                // Configura o 'Id' como auto-incremental
                e.Property(s => s.Id).ValueGeneratedOnAdd();

                e.HasMany(s => s.Items).WithOne().HasForeignKey(i => i.SaleId);
                e.Property(s => s.Status).HasConversion<string>();
            });

            modelBuilder.Entity<SaleItem>(e =>
            {
                e.HasKey(i => i.Id);

                // Configura o 'Id' como auto-incremental
                e.Property(i => i.Id).ValueGeneratedOnAdd();
            });

            // Configuração do MassTransit (Transactional Outbox)
            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();

        }
    }
}
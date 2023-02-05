using DemoMinimalAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DemoMinimalAPI.Data
{
    public class MinimalContextDb : DbContext
    {
        public MinimalContextDb(DbContextOptions<MinimalContextDb> contextOptions) : base(contextOptions) {}

        public DbSet<Provider> Providers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Provider>()
                .HasKey(p => p.Id);

            modelBuilder.Entity<Provider>()
                .Property(p => p.Name)
                .IsRequired()
                .HasColumnType("varchar(200)");

            modelBuilder.Entity<Provider>()
                .Property(p => p.Document)
                .IsRequired()
                .HasColumnType("varchar(14)");

            modelBuilder.Entity<Provider>()
                .ToTable("Providers");

            base.OnModelCreating(modelBuilder);
        }
    }
}

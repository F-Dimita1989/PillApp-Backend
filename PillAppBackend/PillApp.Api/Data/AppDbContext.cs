using Microsoft.EntityFrameworkCore;
using PillApp.Api.Models;

namespace PillApp.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<FarmacoClasseA> FarmaciClasseA => Set<FarmacoClasseA>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FarmacoClasseA>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Aic).IsUnique();
        });
    }
}
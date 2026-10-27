using Microsoft.EntityFrameworkCore;
using Trackpense.Core.Domain;

namespace Trackpense.Core.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Category>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(60).IsRequired();
            e.Property(c => c.Color).HasMaxLength(9);
            e.HasIndex(c => c.Name).IsUnique();
        });

        b.Entity<Transaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Amount).HasColumnType("decimal(18,2)");
            e.Property(t => t.Kind).HasConversion<string>().HasMaxLength(16);
            e.Property(t => t.Note).HasMaxLength(200);
            e.HasIndex(t => t.Date);
            e.HasOne(t => t.Category).WithMany(c => c.Transactions).HasForeignKey(t => t.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.Ignore(t => t.SignedAmount);
        });

        b.Entity<Budget>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Limit).HasColumnType("decimal(18,2)");
            e.HasIndex(x => new { x.CategoryId, x.Month }).IsUnique();
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

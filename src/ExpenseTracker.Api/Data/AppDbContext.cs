using ExpenseTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseAttachment> ExpenseAttachments => Set<ExpenseAttachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Expense>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(10, 2);                  // DECIMAL(10,2)
            e.Property(x => x.CreatedAt).ValueGeneratedOnAdd();             // DB default SYSUTCDATETIME()
        });

        modelBuilder.Entity<ExpenseAttachment>(e =>
        {
            e.Property(x => x.UploadedAt).ValueGeneratedOnAdd();            // DB default SYSUTCDATETIME()
            e.HasOne(x => x.Expense).WithMany(x => x.Attachments).HasForeignKey(x => x.ExpenseId);
        });
    }
}

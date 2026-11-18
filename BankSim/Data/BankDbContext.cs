using BankSim.Models;
using Microsoft.EntityFrameworkCore;

namespace BankSim.Data;

public class BankDbContext : DbContext
{
    public BankDbContext(DbContextOptions<BankDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<BankTransaction> Transactions => Set<BankTransaction>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
        b.Entity<Account>().HasIndex(a => a.Iban).IsUnique();
        b.Entity<BankTransaction>().HasIndex(t => new { t.FromAccountId, t.CreatedAt });
        b.Entity<BankTransaction>().HasIndex(t => new { t.ToAccountId, t.CreatedAt });
    }
}
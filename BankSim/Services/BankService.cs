using BankSim.Data;
using BankSim.Models;
using Microsoft.EntityFrameworkCore;

namespace BankSim.Services;

public class BankService
{
    private readonly IDbContextFactory<BankDbContext> _factory;
    private readonly AuthService _auth;

    public BankService(IDbContextFactory<BankDbContext> factory, AuthService auth)
    {
        _factory = factory;
        _auth = auth;
    }

    private int UserId => _auth.CurrentUser?.Id
        ?? throw new InvalidOperationException("Utilisateur non connecté.");

    public async Task<List<Account>> GetMyAccountsAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Accounts
            .Where(a => a.UserId == UserId && a.Status != AccountStatus.Closed)
            .ToListAsync();
    }

    public async Task<Account> CreateAccountAsync(AccountType type, decimal initialDeposit = 0)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var account = new Account
        {
            UserId = UserId,
            Type = type,
            Iban = await GenerateUniqueIbanAsync(db)
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        if (initialDeposit > 0)
            await DepositAsync(account.Id, initialDeposit, Category.Other, "Dépôt initial");

        return account;
    }

    public async Task DepositAsync(int accountId, decimal amount, Category category, string? description)
    {
        ValidateAmount(amount);
        await using var db = await _factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        var account = await GetActiveOwnedAccountAsync(db, accountId);
        account.Balance += amount;

        db.Transactions.Add(new BankTransaction
        {
            Type = TransactionType.Deposit,
            Amount = amount,
            Category = category,
            Description = description,
            ToAccountId = account.Id
        });

        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task WithdrawAsync(int accountId, decimal amount, Category category, string? description)
    {
        ValidateAmount(amount);
        await using var db = await _factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        var account = await GetActiveOwnedAccountAsync(db, accountId);
        if (account.Balance < amount)
            throw new InvalidOperationException("Solde insuffisant.");

        account.Balance -= amount;

        db.Transactions.Add(new BankTransaction
        {
            Type = TransactionType.Withdrawal,
            Amount = amount,
            Category = category,
            Description = description,
            FromAccountId = account.Id
        });

        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task TransferAsync(int fromAccountId, string toIban, decimal amount,
                                    Category category, string? description)
    {
        ValidateAmount(amount);
        toIban = toIban.Replace(" ", "").ToUpperInvariant();

        await using var db = await _factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        var from = await GetActiveOwnedAccountAsync(db, fromAccountId);
        var to = await db.Accounts.FirstOrDefaultAsync(a => a.Iban == toIban)
                 ?? throw new InvalidOperationException("IBAN destinataire introuvable.");

        if (to.Id == from.Id)
            throw new InvalidOperationException("Les deux comptes doivent être différents.");
        if (to.Status != AccountStatus.Active)
            throw new InvalidOperationException("Le compte destinataire est inactif.");
        if (from.Balance < amount)
            throw new InvalidOperationException("Solde insuffisant.");

        from.Balance -= amount;
        to.Balance += amount;

        db.Transactions.Add(new BankTransaction
        {
            Type = TransactionType.Transfer,
            Amount = amount,
            Category = category,
            Description = description,
            FromAccountId = from.Id,
            ToAccountId = to.Id
        });

        await db.SaveChangesAsync();
        await tx.CommitAsync();   // sans Commit, tout est annulé automatiquement
    }

    public async Task<List<BankTransaction>> GetHistoryAsync(int accountId, int take = 50)
    {
        await using var db = await _factory.CreateDbContextAsync();
        await GetActiveOwnedAccountAsync(db, accountId, requireActive: false);

        return await db.Transactions
            .Where(t => t.FromAccountId == accountId || t.ToAccountId == accountId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    // ---------- helpers ----------

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Le montant doit être positif.");
    }

    private async Task<Account> GetActiveOwnedAccountAsync(BankDbContext db, int accountId,
                                                           bool requireActive = true)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == UserId)
            ?? throw new InvalidOperationException("Compte introuvable.");

        if (requireActive && account.Status != AccountStatus.Active)
            throw new InvalidOperationException("Ce compte n'est pas actif.");

        return account;
    }

    private static async Task<string> GenerateUniqueIbanAsync(BankDbContext db)
    {
        var rnd = Random.Shared;
        while (true)
        {
            var digits = string.Concat(Enumerable.Range(0, 20).Select(_ => rnd.Next(0, 10)));
            var iban = $"TN59{digits}";
            if (!await db.Accounts.AnyAsync(a => a.Iban == iban))
                return iban;
        }
    }
}
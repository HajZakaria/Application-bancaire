namespace BankSim.Models;

public enum AccountType { Checking, Savings }
public enum AccountStatus { Active, Frozen, Closed }
public enum TransactionType { Deposit, Withdrawal, Transfer }
public enum Category { Food, Transport, Shopping, Bills, Salary, Leisure, Other }

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Salt { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Account> Accounts { get; set; } = new();
}

public class Account
{
    public int Id { get; set; }
    public string Iban { get; set; } = "";
    public AccountType Type { get; set; } = AccountType.Checking;
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "TND";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User? User { get; set; }

    public override string ToString() => $"{Type} - {Iban} ({Balance:N2} {Currency})";
}

public class BankTransaction
{
    public int Id { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public Category Category { get; set; } = Category.Other;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? FromAccountId { get; set; }
    public int? ToAccountId { get; set; }
}
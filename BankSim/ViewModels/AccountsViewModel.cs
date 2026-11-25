using BankSim.Models;
using BankSim.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;


namespace BankSim.ViewModels;

public partial class AccountsViewModel : ObservableObject
{
    private readonly BankService _bank;
    private readonly AuthService _auth;

    public AccountsViewModel(BankService bank, AuthService auth)
    {
        _bank = bank;
        _auth = auth;
    }

    public ObservableCollection<Account> Accounts { get; } = new();
    public IList<AccountType> AccountTypes { get; } = Enum.GetValues<AccountType>();
    public IList<Category> Categories { get; } = Enum.GetValues<Category>();

    public string Greeting => $"Bonjour, {_auth.CurrentUser?.Name}";

    [ObservableProperty] private decimal totalBalance;
    [ObservableProperty] private bool hasNoAccounts;


    [ObservableProperty] private AccountType newAccountType = AccountType.Checking;
    [ObservableProperty] private string initialDepositText = "";


    [ObservableProperty] private Account? selectedAccount;
    [ObservableProperty] private string amountText = "";
    [ObservableProperty] private Category selectedCategory = Category.Other;
    [ObservableProperty] private string description = "";

    [ObservableProperty] private string? message;
    [ObservableProperty] private bool isBusy;

    [RelayCommand]
    private Task LoadAsync()
    {
        OnPropertyChanged(nameof(Greeting));
        return RunAsync(() => Task.CompletedTask, null);
    }

    [RelayCommand]
    private Task CreateAccountAsync()
    {
        decimal deposit = 0;
        if (!string.IsNullOrWhiteSpace(InitialDepositText) && !TryParseAmount(InitialDepositText, out deposit))
        {
            Message = "Montant du dépôt initial invalide.";
            return Task.CompletedTask;
        }

        return RunAsync(async () =>
        {
            var account = await _bank.CreateAccountAsync(NewAccountType, deposit);
            InitialDepositText = "";
            SelectedAccount = account; // sélectionne le nouveau compte après rechargement
        }, "Compte créé.");
    }

    [RelayCommand]
    private Task DepositAsync() => MoveMoneyAsync(isDeposit: true);

    [RelayCommand]
    private Task WithdrawAsync() => MoveMoneyAsync(isDeposit: false);

    [RelayCommand]
    private async Task LogoutAsync()
    {
        _auth.Logout();
        Accounts.Clear();
        await Shell.Current.GoToAsync("//login");
    }

    private Task MoveMoneyAsync(bool isDeposit)
    {
        if (SelectedAccount is null)
        {
            Message = "Choisissez un compte.";
            return Task.CompletedTask;
        }
        if (!TryParseAmount(AmountText, out var amount))
        {
            Message = "Montant invalide.";
            return Task.CompletedTask;
        }

        var accountId = SelectedAccount.Id;
        var desc = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim();

        return RunAsync(async () =>
        {
            if (isDeposit)
                await _bank.DepositAsync(accountId, amount, SelectedCategory, desc);
            else
                await _bank.WithdrawAsync(accountId, amount, SelectedCategory, desc);

            AmountText = "";
            Description = "";
        }, isDeposit ? "Dépôt effectué." : "Retrait effectué.");
    }

    private async Task RunAsync(Func<Task> action, string? successMessage)
    {
        if (IsBusy) return;
        Message = null;
        try
        {
            IsBusy = true;
            await action();
            await ReloadAccountsAsync();
            Message = successMessage;
        }
        catch (InvalidOperationException ex) { Message = ex.Message; }
        catch (Exception ex) { Message = $"Erreur : {ex.Message}"; }
        finally { IsBusy = false; }
    }

    private async Task ReloadAccountsAsync()
    {
        var keepId = SelectedAccount?.Id;
        var list = await _bank.GetMyAccountsAsync();

        Accounts.Clear();
        foreach (var a in list) Accounts.Add(a);

        SelectedAccount = Accounts.FirstOrDefault(a => a.Id == keepId) ?? Accounts.FirstOrDefault();
        TotalBalance = Accounts.Sum(a => a.Balance);
        HasNoAccounts = Accounts.Count == 0;
    }


    private static bool TryParseAmount(string? text, out decimal amount) =>
        decimal.TryParse((text ?? "").Trim().Replace(',', '.'),
                         NumberStyles.Number, CultureInfo.InvariantCulture, out amount)
        && amount > 0;
}
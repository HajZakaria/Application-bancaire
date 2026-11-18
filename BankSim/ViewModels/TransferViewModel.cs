using System.Collections.ObjectModel;
using System.Globalization;
using BankSim.Models;
using BankSim.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BankSim.ViewModels;

public partial class TransferViewModel : ObservableObject
{
    private readonly BankService _bank;

    public TransferViewModel(BankService bank) => _bank = bank;

    public ObservableCollection<Account> Accounts { get; } = new();
    public List<Category> Categories { get; } = Enum.GetValues<Category>().ToList();

    [ObservableProperty] private Account? selectedAccount;
    [ObservableProperty] private Category selectedCategory = Category.Other;
    [ObservableProperty] private string destinationIban = "";
    [ObservableProperty] private string amountText = "";
    [ObservableProperty] private string? description;
    [ObservableProperty] private string message = "";
    [ObservableProperty] private bool isBusy;

    [RelayCommand]
    public async Task LoadAsync()
    {
        Accounts.Clear();
        foreach (var a in await _bank.GetMyAccountsAsync())
            Accounts.Add(a);

        SelectedAccount ??= Accounts.FirstOrDefault();
    }

    [RelayCommand]
    private async Task TransferAsync()
    {
        if (IsBusy) return;
        Message = "";

        if (SelectedAccount is null)
        {
            Message = "Choisis un compte source.";
            return;
        }

        var normalized = AmountText.Replace(',', '.');
        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            Message = "Montant invalide.";
            return;
        }

        try
        {
            IsBusy = true;
            await _bank.TransferAsync(SelectedAccount.Id, DestinationIban, amount,
                                      SelectedCategory, Description);

            Message = "Virement effectué ✔";
            AmountText = "";
            DestinationIban = "";
            Description = null;
            await LoadAsync();
        }
        catch (InvalidOperationException ex)
        {
            Message = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
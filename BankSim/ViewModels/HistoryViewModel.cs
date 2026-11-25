using BankSim.Models;
using BankSim.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BankSim.ViewModels;

// Une ligne prête à afficher
public record TransactionRow(string Title, string Details, string AmountText,
                             Color AmountColor, string DateText);

public partial class HistoryViewModel : ObservableObject
{
    private readonly BankService _bank;

    public HistoryViewModel(BankService bank) => _bank = bank;

    public ObservableCollection<Account> Accounts { get; } = new();
    public ObservableCollection<TransactionRow> Rows { get; } = new();

    [ObservableProperty] private Account? selectedAccount;
    [ObservableProperty] private string? message;
    [ObservableProperty] private bool isBusy;

    // Appelé automatiquement quand on change de compte dans le Picker
    partial void OnSelectedAccountChanged(Account? value) => _ = LoadHistoryAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            Message = null;
            var keepId = SelectedAccount?.Id;
            var list = await _bank.GetMyAccountsAsync();

            Accounts.Clear();
            foreach (var a in list) Accounts.Add(a);

            SelectedAccount = Accounts.FirstOrDefault(a => a.Id == keepId) ?? Accounts.FirstOrDefault();
            if (SelectedAccount is null) Rows.Clear();
        }
        catch (Exception ex) { Message = ex.Message; }
    }

    private async Task LoadHistoryAsync()
    {
        if (SelectedAccount is null) return;
        var id = SelectedAccount.Id;

        try
        {
            IsBusy = true;
            var transactions = await _bank.GetHistoryAsync(id);

            Rows.Clear();
            foreach (var t in transactions)
            {
                bool incoming = t.ToAccountId == id;

                string title = t.Type switch
                {
                    TransactionType.Deposit => "Dépôt",
                    TransactionType.Withdrawal => "Retrait",
                    _ => incoming ? "Virement reçu" : "Virement émis"
                };

                string details = string.IsNullOrWhiteSpace(t.Description)
                    ? t.Category.ToString()
                    : $"{t.Category} • {t.Description}";

                Rows.Add(new TransactionRow(
                    title,
                    details,
                    $"{(incoming ? "+" : "−")} {t.Amount:N2} TND",
                    incoming ? Colors.Green : Colors.Red,
                    t.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")));
            }
        }
        catch (Exception ex) { Message = ex.Message; }
        finally { IsBusy = false; }
    }
}
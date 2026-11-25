using System.Collections.ObjectModel;
using System.Globalization;
using BankSim.Models;
using BankSim.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BankSim.ViewModels;

public record CategoryRow(string Name, string AmountText, double Progress);
public record MonthRow(string MonthName, string IncomeText, string ExpensesText,
                       double IncomeProgress, double ExpensesProgress);

public partial class StatsViewModel : ObservableObject
{
    private static readonly CultureInfo Fr = new("fr-FR");

    private readonly BankService _bank;
    private readonly StatsService _stats;

    public StatsViewModel(BankService bank, StatsService stats)
    {
        _bank = bank;
        _stats = stats;
    }

    public ObservableCollection<Account> Accounts { get; } = new();
    public ObservableCollection<CategoryRow> Categories { get; } = new();
    public ObservableCollection<MonthRow> Months { get; } = new();

    [ObservableProperty] private Account? selectedAccount;
    [ObservableProperty] private string monthTotalText = "";
    [ObservableProperty] private bool hasNoSpending;
    [ObservableProperty] private string? message;

    partial void OnSelectedAccountChanged(Account? value) => _ = LoadStatsAsync();

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
        }
        catch (Exception ex) { Message = ex.Message; }
    }

    private async Task LoadStatsAsync()
    {
        Categories.Clear();
        Months.Clear();
        if (SelectedAccount is null) return;
        var id = SelectedAccount.Id;

        try
        {
            // ---- Dépenses par catégorie (mois en cours) ----
            var byCategory = await _stats.SpendingByCategoryAsync(id);
            var total = byCategory.Sum(c => c.Total);
            var maxCat = byCategory.Count > 0 ? byCategory.Max(c => c.Total) : 1;

            foreach (var c in byCategory)
                Categories.Add(new CategoryRow(
                    c.Category.ToString(),
                    $"{c.Total:N2} TND",
                    (double)(c.Total / maxCat)));

            MonthTotalText = $"Total dépensé ce mois : {total:N2} TND";
            HasNoSpending = byCategory.Count == 0;

            // ---- Entrées / sorties sur 6 mois ----
            var flows = await _stats.MonthlyFlowAsync(id);
            var maxFlow = flows.Count > 0
                ? Math.Max(flows.Max(f => f.Income), flows.Max(f => f.Expenses))
                : 1;
            if (maxFlow == 0) maxFlow = 1;

            foreach (var f in flows)
                Months.Add(new MonthRow(
                    Fr.TextInfo.ToTitleCase(new DateTime(f.Year, f.Month, 1).ToString("MMMM yyyy", Fr)),
                    $"+ {f.Income:N2} TND",
                    $"− {f.Expenses:N2} TND",
                    (double)(f.Income / maxFlow),
                    (double)(f.Expenses / maxFlow)));
        }
        catch (Exception ex) { Message = ex.Message; }
    }
}
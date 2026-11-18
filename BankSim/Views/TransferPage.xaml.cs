using BankSim.ViewModels;

namespace BankSim.Views;

public partial class TransferPage : ContentPage
{
    private readonly TransferViewModel _vm;

    public TransferPage(TransferViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCommand.ExecuteAsync(null);
    }
}
using BankSim.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BankSim.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _auth;

    public LoginViewModel(AuthService auth) => _auth = auth;

    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private string? message;
    [ObservableProperty] private bool isBusy;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy) return;
        Message = null;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            Message = "Email et mot de passe requis.";
            return;
        }

        try
        {
            IsBusy = true;
            await _auth.LoginAsync(Email, Password);
            Password = "";
            await Shell.Current.GoToAsync("//main");
        }
        catch (InvalidOperationException ex) { Message = ex.Message; }
        catch (Exception ex) { Message = $"Erreur : {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task GoToRegisterAsync() => Shell.Current.GoToAsync("register");
}
using BankSim.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;


namespace BankSim.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly AuthService _auth;

    public RegisterViewModel(AuthService auth) => _auth = auth;

    [ObservableProperty] private string name = "";
    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private string confirmPassword = "";
    [ObservableProperty] private string? message;
    [ObservableProperty] private bool isBusy;

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (IsBusy) return;
        Message = null;

        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Email))
        {
            Message = "Nom et email requis.";
            return;
        }
        if (!Email.Contains('@'))
        {
            Message = "Email invalide.";
            return;
        }
        if (Password != ConfirmPassword)
        {
            Message = "Les mots de passe ne correspondent pas.";
            return;
        }

        try
        {
            IsBusy = true;
            await _auth.RegisterAsync(Name, Email, Password); // connecte aussi l'utilisateur
            await Shell.Current.GoToAsync("//main");
        }
        catch (InvalidOperationException ex) { Message = ex.Message; }
        catch (Exception ex) { Message = $"Erreur : {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private Task GoBackAsync() => Shell.Current.GoToAsync("..");
}
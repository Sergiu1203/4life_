using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using _4Life.Data;
using Microsoft.EntityFrameworkCore;

namespace _4Life.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly AppDbContext _context;

    [ObservableProperty] private string email;
    [ObservableProperty] private string password;

    public LoginViewModel(AppDbContext context)
    {
        _context = context;
    }

    [RelayCommand]
    async Task Login()
    {
        if (string.IsNullOrWhiteSpace(this.email) || string.IsNullOrWhiteSpace(this.password)) return;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == this.email && u.Password == this.password);

        if (user != null)
        {
            await Shell.Current.DisplayAlert("Succes", $"Welcome, {user.Role}!", "OK");
        }
        else
        {
            await Shell.Current.DisplayAlert("Error", "Invalid email or password", "OK");
        }
    }

    [RelayCommand]
    async Task GoToRegister()
    {
        await Shell.Current.GoToAsync("//RegisterPage");
    }
}
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

        if (Email == "admin" && Password == "admin")
        {
            await Shell.Current.GoToAsync("//AdminDashboardPage");
            return;
        }

        if (user == null)
        {
            await Shell.Current.DisplayAlert("Error", "Invalid email or password", "OK");
            return;
        }

        Preferences.Default.Set("CurrentUserId", user.Id);
        await Shell.Current.DisplayAlert("Success", $"Welcome, {user.Role}!", "OK");

        if (user.Role == "Patient")
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
            string displayName = patient?.FullName ?? user.Email;
            await Shell.Current.GoToAsync($"//DashboardPage?name={displayName}");
        }
        else if (user.Role == "Doctor")
        {
            // await Shell.Current.GoToAsync("//DoctorDashboardPage");
            await Shell.Current.GoToAsync("//DoctorDashboardPage");
        }

    }

    [RelayCommand]
    async Task GoToRegister()
    {
        await Shell.Current.GoToAsync("//RegisterPage");
    }
}
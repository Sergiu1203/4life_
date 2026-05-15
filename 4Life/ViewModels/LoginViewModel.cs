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
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password)) return;

        // Admin hardcodat
        if (Email == "admin" && Password == "admin")
        {
            await Shell.Current.DisplayAlert("Welcome", "Hello, Admin!", "OK");
            await Shell.Current.GoToAsync("//AdminDashboardPage");
            return;
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == Email && u.Password == Password);

        if (user == null)
        {
            await Shell.Current.DisplayAlert("Error", "Invalid email or password.", "OK");
            return;
        }

        Preferences.Default.Set("CurrentUserId", user.Id);

        if (user.Role == "Patient")
        {
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == user.Id);
            string displayName = patient?.FullName ?? user.Email;

            await Shell.Current.DisplayAlert("Welcome back! 👋", $"Hello, {displayName}!", "Let's go");

            // Verificam daca pacientul a facut check-in-ul de azi
            string checkInKey  = $"CheckIn_{patient?.Id}_{DateTime.Today:yyyy-MM-dd}";
            bool   doneToday   = Preferences.Default.Get(checkInKey, false);

            if (!doneToday && patient != null)
            {
                // Navigam mai intai la dashboard, apoi deschidem check-in-ul
                await Shell.Current.GoToAsync($"//DashboardPage?name={displayName}");
                await Shell.Current.GoToAsync(
                    $"DailyCheckInPage?patientId={patient.Id}");
            }
            else
            {
                await Shell.Current.GoToAsync($"//DashboardPage?name={displayName}");
            }
        }
        else if (user.Role == "Doctor")
        {
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.UserId == user.Id);
            string displayName = doctor?.FullName ?? user.Email;

            await Shell.Current.DisplayAlert("Welcome back! 👋", $"Hello, Dr. {displayName}!", "Let's go");
            await Shell.Current.GoToAsync("//DoctorDashboardPage");
        }
    }

    [RelayCommand]
    async Task GoToForgotPassword()
        => await Shell.Current.GoToAsync(nameof(_4Life.Views.ForgotPasswordPage));

    [RelayCommand]
    async Task GoToRegister()
        => await Shell.Current.GoToAsync("//RegisterPage");
}

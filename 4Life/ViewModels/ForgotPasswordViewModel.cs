using _4Life.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace _4Life.ViewModels
{
    public partial class ForgotPasswordViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        // Pasul 1: introdu email
        // Pasul 2: dupa verificare, introdu parola noua
        [ObservableProperty] private string email = string.Empty;
        [ObservableProperty] private string newPassword = string.Empty;
        [ObservableProperty] private string confirmPassword = string.Empty;

        [ObservableProperty] private bool emailVerified = false;
        [ObservableProperty] private string statusMessage = string.Empty;
        [ObservableProperty] private Color statusColor = Colors.Gray;

        [ObservableProperty] private string passwordStrengthMessage = string.Empty;
        [ObservableProperty] private Color strengthColor = Colors.Gray;

        public ForgotPasswordViewModel(AppDbContext context)
        {
            _context = context;
        }

        partial void OnNewPasswordChanged(string value) => UpdateStrength(value);

        private void UpdateStrength(string pwd)
        {
            if (string.IsNullOrEmpty(pwd)) { PasswordStrengthMessage = string.Empty; return; }
            if (pwd.Length < 8) { PasswordStrengthMessage = "Too short (min 8 chars)"; StrengthColor = Colors.Red; return; }

            bool up  = pwd.Any(char.IsUpper);
            bool lo  = pwd.Any(char.IsLower);
            bool di  = pwd.Any(char.IsDigit);
            bool sp  = pwd.Any(c => !char.IsLetterOrDigit(c));
            int score = (up?1:0)+(lo?1:0)+(di?1:0)+(sp?1:0);

            (PasswordStrengthMessage, StrengthColor) = score switch
            {
                <=2 => ("Weak", Colors.Orange),
                3   => ("Medium", Colors.YellowGreen),
                _   => ("Strong", Colors.Green)
            };
        }

        // Pasul 1 — verifica email-ul
        [RelayCommand]
        async Task VerifyEmail()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                StatusMessage = "Please enter your email.";
                StatusColor   = Colors.Red;
                return;
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == Email.Trim());

            if (user == null)
            {
                StatusMessage = "No account found with this email.";
                StatusColor   = Colors.Red;
                return;
            }

            EmailVerified = true;
            StatusMessage = "Email verified! Set your new password below.";
            StatusColor   = Colors.Green;
        }

        // Pasul 2 — seteaza parola noua
        [RelayCommand]
        async Task ResetPassword()
        {
            bool valid = NewPassword?.Length >= 8 &&
                         NewPassword.Any(char.IsUpper) &&
                         NewPassword.Any(char.IsLower) &&
                         NewPassword.Any(char.IsDigit) &&
                         NewPassword.Any(c => !char.IsLetterOrDigit(c));

            if (!valid)
            {
                StatusMessage = "Password must be 8+ chars with upper, lower, number and special character.";
                StatusColor   = Colors.Red;
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                StatusMessage = "Passwords do not match.";
                StatusColor   = Colors.Red;
                return;
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == Email.Trim());

            if (user == null) return;

            user.Password = NewPassword;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            await Shell.Current.DisplayAlert("Done", "Password updated successfully!", "OK");
            await Shell.Current.GoToAsync("//LoginPage");
        }

        [RelayCommand]
        async Task GoBack() => await Shell.Current.GoToAsync("//LoginPage");
    }
}

using _4Life.Data;
using _4Life.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _4Life.ViewModels
{
    public partial class RegisterViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty] private string email;
        [ObservableProperty] private string password;
        [ObservableProperty] private string fullName;
        [ObservableProperty] private string selectedRole; 
        [ObservableProperty] private int? age;
        [ObservableProperty] private string medicalId;
        [ObservableProperty] private string specialization;
        [ObservableProperty] private Doctor selectedDoctor;

        [ObservableProperty]
        private string passwordStrengthMessage = "The password has to be minimum 8 characters long and contain upper case and lower case letters, numbers and special characters";

        [ObservableProperty]
        private Color strengthColor = Colors.Gray;
        public ObservableCollection<Doctor> AvailableDoctors { get; set; } = new();

        
        public List<string> Roles { get; } = new() { "Patient", "Doctor" };

        public RegisterViewModel(AppDbContext context)
        {
            _context = context;
            LoadDoctors();
        }

        partial void OnPasswordChanged(string value)
        {
            UpdatePasswordStrength(value);
        }

        private void UpdatePasswordStrength(string pwd)
        {
            if (string.IsNullOrEmpty(pwd))
            {
                PasswordStrengthMessage = "Password is required";
                StrengthColor = Colors.Gray;
                return;
            }

            if (pwd.Length < 8)
            {
                PasswordStrengthMessage = "Too short (min 8 characters)";
                StrengthColor = Colors.Red;
                return;
            }

            bool hasUpper = pwd.Any(char.IsUpper);
            bool hasLower = pwd.Any(char.IsLower);
            bool hasDigit = pwd.Any(char.IsDigit);
            bool hasSpecial = pwd.Any(ch => !char.IsLetterOrDigit(ch));

            int criteriaMet = (hasUpper ? 1 : 0) + (hasLower ? 1 : 0) + (hasDigit ? 1 : 0) + (hasSpecial ? 1 : 0);

            if (criteriaMet <= 2)
            {
                PasswordStrengthMessage = "Weak password (add symbols/numbers)";
                StrengthColor = Colors.Orange;
            }
            else if (criteriaMet == 3)
            {
                PasswordStrengthMessage = "Medium password";
                StrengthColor = Colors.YellowGreen;
            }
            else if (criteriaMet == 4)
            {
                PasswordStrengthMessage = "Strong password";
                StrengthColor = Colors.Green;
            }
        }

        private async void LoadDoctors()
        {
            var doctors = await _context.Doctors.ToListAsync();
            foreach (var d in doctors) AvailableDoctors.Add(d);
}
        [RelayCommand]
        async Task RegisterUser()
        {
            if (string.IsNullOrWhiteSpace(Email) || !Email.Contains("@"))
            {
                await Shell.Current.DisplayAlert("Error", "Please enter a valid email address containing '@'", "OK");
                return;
            }

            bool isValidPassword = Password?.Length >= 8 &&
                                   Password.Any(char.IsUpper) &&
                                   Password.Any(char.IsLower) &&
                                   Password.Any(char.IsDigit) &&
                                   Password.Any(ch => !char.IsLetterOrDigit(ch));

            if (!isValidPassword)
            {
                await Shell.Current.DisplayAlert("Weak Password",
                    "Password must be at least 8 characters long and contain: Uppercase, Lowercase, Number, and Special Character.", "OK");
                return;
            }
            try
            {
                var newUser = new User
                {
                    Email = this.email,
                    Password = this.password,
                    Role = this.selectedRole
                };

                if (selectedRole == "Patient")
                {
                    var newPatient = new Patient
                    {
                        FullName = this.fullName,
                        User = newUser, // Link the object directly [cite: 1]
                        Age = this.age,
                        // Assign the ID from the selected doctor object
                        DoctorId = SelectedDoctor?.Id
                    };
                    _context.Patients.Add(newPatient);
                }
                else if (selectedRole == "Doctor")
                {
                    var newDoctor = new Doctor
                    {
                        FullName = this.fullName,
                        User = newUser,
                        MedicalId = this.medicalId,
                        Specialization = this.specialization
                    };
                    _context.Doctors.Add(newDoctor);
                }

                await _context.SaveChangesAsync();
                await Shell.Current.DisplayAlert("Success", "Account created!", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
            }
            catch (Exception ex)
            {
                // This will tell us if it's a "FOREIGN KEY constraint failed" or "NOT NULL constraint"
                var innerError = ex.InnerException?.Message ?? ex.Message;
                await Shell.Current.DisplayAlert("Database Error", innerError, "OK");
            }
        }
        partial void OnSelectedRoleChanged(string value)
        {
            OnPropertyChanged(nameof(IsDoctor));
            OnPropertyChanged(nameof(IsPatient));
        }

        public bool IsDoctor => SelectedRole == "Doctor";
        public bool IsPatient => SelectedRole == "Patient";
    }
}

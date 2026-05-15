using _4Life.Data;
using _4Life.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

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

        [ObservableProperty]
        private string passwordStrengthMessage = "Password must be min 8 chars with upper, lower, number and special character.";

        [ObservableProperty]
        private Color strengthColor = Colors.Gray;

        // Lista tuturor doctorilor disponibili, cu flag IsSelected pentru checkboxuri
        public ObservableCollection<DoctorSelectionItem> AvailableDoctors { get; set; } = new();

        public List<string> Roles { get; } = new() { "Patient", "Doctor" };

        public RegisterViewModel(AppDbContext context)
        {
            _context = context;
            LoadDoctors();
        }

        partial void OnPasswordChanged(string value) => UpdatePasswordStrength(value);

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

            bool hasUpper   = pwd.Any(char.IsUpper);
            bool hasLower   = pwd.Any(char.IsLower);
            bool hasDigit   = pwd.Any(char.IsDigit);
            bool hasSpecial = pwd.Any(ch => !char.IsLetterOrDigit(ch));
            int score = (hasUpper ? 1 : 0) + (hasLower ? 1 : 0) + (hasDigit ? 1 : 0) + (hasSpecial ? 1 : 0);

            (PasswordStrengthMessage, StrengthColor) = score switch
            {
                <= 2 => ("Weak password (add symbols/numbers)", Colors.Orange),
                3    => ("Medium password", Colors.YellowGreen),
                _    => ("Strong password", Colors.Green)
            };
        }

        private async void LoadDoctors()
        {
            var doctors = await _context.Doctors.ToListAsync();
            foreach (var d in doctors)
                AvailableDoctors.Add(new DoctorSelectionItem { Doctor = d, IsSelected = false });
        }

        [RelayCommand]
        async Task RegisterUser()
        {
            if (string.IsNullOrWhiteSpace(Email) || !Email.Contains("@"))
            {
                await Shell.Current.DisplayAlert("Error", "Please enter a valid email address.", "OK");
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
                    "Password must be at least 8 characters and contain uppercase, lowercase, number and special character.", "OK");
                return;
            }

            try
            {
                var newUser = new User
                {
                    Email    = this.Email,
                    Password = this.Password,
                    Role     = this.SelectedRole
                };

                if (SelectedRole == "Patient")
                {
                    var selectedDoctors = AvailableDoctors.Where(d => d.IsSelected).ToList();

                    var newPatient = new Patient
                    {
                        FullName = this.FullName,
                        User     = newUser,
                        Age      = this.Age,
                        // Doctorul principal = primul selectat (sau null daca nu s-a selectat niciunul)
                        DoctorId = selectedDoctors.FirstOrDefault()?.Doctor.Id
                    };

                    _context.Patients.Add(newPatient);
                    await _context.SaveChangesAsync(); // salvam ca sa avem newPatient.Id

                    // Adaugam toate legaturile many-to-many
                    foreach (var item in selectedDoctors)
                    {
                        _context.PatientDoctors.Add(new PatientDoctor
                        {
                            PatientId = newPatient.Id,
                            DoctorId  = item.Doctor.Id
                        });
                    }

                    await _context.SaveChangesAsync();
                }
                else if (SelectedRole == "Doctor")
                {
                    var newDoctor = new Doctor
                    {
                        FullName       = this.FullName,
                        User           = newUser,
                        MedicalId      = this.MedicalId,
                        Specialization = this.Specialization
                    };
                    _context.Doctors.Add(newDoctor);
                    await _context.SaveChangesAsync();
                }

                await Shell.Current.DisplayAlert("Success", "Account created!", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
            }
            catch (Exception ex)
            {
                var innerError = ex.InnerException?.Message ?? ex.Message;
                await Shell.Current.DisplayAlert("Database Error", innerError, "OK");
            }
        }

        partial void OnSelectedRoleChanged(string value)
        {
            OnPropertyChanged(nameof(IsDoctor));
            OnPropertyChanged(nameof(IsPatient));
        }

        public bool IsDoctor  => SelectedRole == "Doctor";
        public bool IsPatient => SelectedRole == "Patient";
    }
}

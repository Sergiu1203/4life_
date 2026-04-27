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
        public ObservableCollection<Doctor> AvailableDoctors { get; set; } = new();


        public List<string> Roles { get; } = new() { "Pacient", "Doctor" };

        public RegisterViewModel(AppDbContext context)
        {
            _context = context;
            LoadDoctors();
        }

        private async void LoadDoctors()
        {
            var doctors = await _context.Doctors.ToListAsync();
            foreach (var d in doctors) AvailableDoctors.Add(d);
}
        [RelayCommand]
        async Task RegisterUser()
        {
            try
            {
                var newUser = new User
                {
                    Email = this.email,
                    Password = this.password,
                    Role = this.selectedRole
                };

                if (selectedRole == "Pacient")
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
        public bool IsPatient => SelectedRole == "Pacient";
    }
}

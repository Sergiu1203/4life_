using _4Life.Models;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using _4Life.Data;
using System;
using System.Collections.Generic;
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

        public List<string> Roles { get; } = new() { "Pacient", "Doctor" };

        public RegisterViewModel(AppDbContext context)
        {
            _context = context;
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

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                if (selectedRole == "Pacient")
                {
                    var newPatient = new Patient
                    {
                        FullName = this.fullName,
                        UserId = newUser.Id,
                        Age = this.age
                    };
                    _context.Patients.Add(newPatient);
                }
                else if (selectedRole == "Doctor")
                {
                    var newDoctor = new Doctor
                    {
                        FullName = this.fullName,
                        UserId = newUser.Id,
                        MedicalId = this.medicalId,
                        Specialization = this.specialization
                    };
                    _context.Doctors.Add(newDoctor);
                }

                await _context.SaveChangesAsync();
                await Shell.Current.DisplayAlert("Succes", "Account succsefully created!", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "An error occured: " + ex.Message, "OK");
            }
        }
    }
}

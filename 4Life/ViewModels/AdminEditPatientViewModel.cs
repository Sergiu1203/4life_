using _4Life.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using static Java.Util.Jar.Attributes;

namespace _4Life.ViewModels
{
    [QueryProperty(nameof(PatientId), "patientId")]
    public partial class AdminEditPatientViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        [ObservableProperty] private int patientId;
        [ObservableProperty] private string name;
        [ObservableProperty] private string email;

        public AdminEditPatientViewModel(AppDbContext context) => _context = context;

        // Metodă apelată când PatientId este setat
        partial void OnPatientIdChanged(int value) => LoadPatientData(value);

        async void LoadPatientData(int id)
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == id);
            if (patient != null)
            {
                Name = patient.FullName;
                // Presupunând că ai Email în modelul Patient sau îl iei din User
                var user = await _context.Users.FindAsync(patient.UserId);
                Email = user?.Email;
            }
        }

        [RelayCommand]
        async Task SaveChanges()
        {
            var patient = await _context.Patients.FindAsync(PatientId);
            var user = await _context.Users.FindAsync(patient?.UserId);

            if (patient != null && user != null)
            {
                patient.FullName = Name;
                user.Email = Email;
                //user.FullName = Name; // Actualizăm în ambele părți

                await _context.SaveChangesAsync();
                await Shell.Current.DisplayAlert("Success", "Patient updated", "OK");
                await Shell.Current.GoToAsync("..");
            }
        }
    }
}

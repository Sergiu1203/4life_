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
    public partial class AdminDashboardViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        public ObservableCollection<Patient> AllPatients { get; set; } = new();

        public AdminDashboardViewModel(AppDbContext context)
        {
            _context = context;
        }

        public async Task LoadAllPatients()
        {
            var patients = await _context.Patients.AsNoTracking().ToListAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AllPatients.Clear();
                foreach (var p in patients) AllPatients.Add(p);
            });
        }

        [RelayCommand]
        async Task DeletePatient(Patient patient)
        {
            bool confirm = await Shell.Current.DisplayAlert("Warning", $"Delete {patient.FullName} permanently?", "Yes", "No");
            if (confirm)
            {
                // Căutăm și ștergem și User-ul asociat ca să nu rămână date orfane
                var user = await _context.Users.FindAsync(patient.UserId);
                if (user != null) _context.Users.Remove(user);

                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync();
                await LoadAllPatients();
            }
        }

        [RelayCommand]
        async Task EditPatient(Patient patient)
        {
            // Navigăm către o pagină de editare, trimițând ID-ul pacientului
            await Shell.Current.GoToAsync($"AdminEditPatientPage?patientId={patient.Id}");
        }

        [RelayCommand]
        async Task Logout() => await Shell.Current.GoToAsync("//LoginPage");
    }
}

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
    public partial class DoctorDashboardViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        public ObservableCollection<Patient> MyPatients { get; set; } = new();

        public DoctorDashboardViewModel(AppDbContext context)
        {
            _context = context;
        }
        /*
        public async Task LoadPatients(int userId)
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);

            if (doctor == null) return;

            var patients = await _context.Patients
                .Where(p => p.DoctorId == doctor.Id)
                .ToListAsync();

            MyPatients.Clear();
            foreach (var p in patients)
            {
                MyPatients.Add(p);
            }
        }
        */
        
        public async Task LoadPatients(int userId)
        {
            var doctor = await _context.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null) return;


            var patients = await _context.Patients.AsNoTracking()
                .Where(p => p.DoctorId == doctor.Id)
                .Include(p => p.PrescribedMedicines)
                .ToListAsync();
            /*
            MyPatients.Clear();
            foreach (var p in patients)
            {
                MyPatients.Add(p);
            }
            */

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MyPatients.Clear();
                foreach (var p in patients)
                {
                    MyPatients.Add(p);
                }
            });
        }

        /*
        [RelayCommand]
        public async Task DeleteMedicine(Medicine med)
        {
            if (med == null) return;

            bool confirm = await Shell.Current.DisplayAlert("Confirm", $"Remove {med.Name} from patient's plan?", "Yes", "No");

            if (confirm)
            {
                _context.Medicines.Remove(med);
                await _context.SaveChangesAsync();

                int currentUserId = Preferences.Default.Get("CurrentUserId", 0);
                await LoadPatients(currentUserId);
            }
        }
        */

        [RelayCommand]
        public async Task DeleteMedicine(Medicine med)
        {
            if (med == null) return;

            bool confirm = await Shell.Current.DisplayAlert("Confirm",
                $"Remove {med.Name} from patient's plan?", "Yes", "No");

            if (confirm)
            {
                try
                {
                    // 1. Find the local tracked version if it exists, or fetch it fresh
                    // This ensures we are deleting the version EF currently 'owns'
                    var trackedMed = await _context.Medicines.FindAsync(med.Id);

                    if (trackedMed != null)
                    {
                        _context.Medicines.Remove(trackedMed);
                        await _context.SaveChangesAsync();
                    }

                    // 2. Refresh the UI using your LoadPatients method
                    int currentUserId = Preferences.Default.Get("CurrentUserId", 0);
                    await LoadPatients(currentUserId);
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Error", "Could not delete: " + ex.Message, "OK");
                }
            }
        }

        [RelayCommand]
        async Task GoToPrescribe(Patient patient)
        {
            // Remove the '//' to use the relative route you just registered
            await Shell.Current.GoToAsync($"PrescribePage?patientId={patient.Id}");
        }

        [RelayCommand]
        async Task GoToEditPrescription(Medicine med)
        {
            // We pass the Medicine ID so the next page knows we are EDITING, not creating new
            await Shell.Current.GoToAsync($"PrescribePage?medicineId={med.Id}");
        }

        public async Task CheckForEmergencies(int userId)
        {
            // 1. Find the Doctor record associated with the logged-in User ID
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);

            if (doctor == null) return;

            // 2. Find any patient assigned to THIS doctor ID who has an active alert
            // Use .AsNoTracking() to ensure we get the latest data from the DB
            var patientInCrisis = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.DoctorId == doctor.Id && p.hasActiveAlert == true);

            if (patientInCrisis != null)
            {
                // 3. Show the alert on the Main Thread to avoid UI crashes
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.DisplayAlert("🚨 EMERGENCY ALERT",
                        $"Patient: {patientInCrisis.FullName}\n\nMessage: {patientInCrisis.emergencyMessage}", "OK");
                });

                // 4. Clear the alert so it doesn't pop up again
                // We need a fresh tracked instance to update
                var patientToUpdate = await _context.Patients.FindAsync(patientInCrisis.Id);
                if (patientToUpdate != null)
                {
                    patientToUpdate.hasActiveAlert = false;
                    patientToUpdate.emergencyMessage = string.Empty;

                    _context.Patients.Update(patientToUpdate);
                    await _context.SaveChangesAsync();
                }
            }
        }

        [RelayCommand]
        async Task Logout()
        {
            // 1. Afișăm fereastra de confirmare
            bool answer = await Shell.Current.DisplayAlert("Logout",
                "Are you sure you want to logout?", "Yes", "No");

            // 2. Dacă utilizatorul a ales "Yes" (true), procedăm la logout
            if (answer)
            {
                // Ștergem ID-ul salvat
                Preferences.Default.Remove("CurrentUserId");

                // Navigăm înapoi la pagina de Login folosind ruta absolută
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }
    }
}

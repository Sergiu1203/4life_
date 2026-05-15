using _4Life.Data;
using _4Life.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace _4Life.ViewModels
{
    public partial class AdminDashboardViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        public ObservableCollection<Patient> AllPatients { get; set; } = new();
        public ObservableCollection<Doctor>  AllDoctors  { get; set; } = new();

        public AdminDashboardViewModel(AppDbContext context)
        {
            _context = context;
        }

        public async Task LoadAllData()
        {
            var patients = await _context.Patients.AsNoTracking().ToListAsync();
            var doctors  = await _context.Doctors.AsNoTracking().ToListAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                AllPatients.Clear();
                foreach (var p in patients) AllPatients.Add(p);

                AllDoctors.Clear();
                foreach (var d in doctors) AllDoctors.Add(d);
            });
        }

        public async Task LoadAllPatients() => await LoadAllData();

        // ---------------------------------------------------------------
        // DELETE PACIENT (sterge complet din DB)
        // ---------------------------------------------------------------
        [RelayCommand]
        async Task DeletePatient(Patient patient)
        {
            bool confirm = await Shell.Current.DisplayAlert("Warning",
                $"Permanently delete {patient.FullName} from the system?", "Yes", "No");
            if (!confirm) return;

            var user = await _context.Users.FindAsync(patient.UserId);
            if (user != null) _context.Users.Remove(user);

            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();
            await LoadAllData();
        }

        // ---------------------------------------------------------------
        // DELETE DOCTOR (#1 admin)
        // Pacientii lui sunt reatribuiti unui doctor cu ACEEASI specializare
        // Daca nu exista, oricare alt doctor disponibil
        // ---------------------------------------------------------------
        [RelayCommand]
        async Task DeleteDoctor(Doctor doctor)
        {
            bool confirm = await Shell.Current.DisplayAlert("Warning",
                $"Delete Dr. {doctor.FullName}?\n\nPatients will be reassigned to a doctor with the same specialization.",
                "Yes, Delete", "Cancel");
            if (!confirm) return;

            try
            {
                // 1. Gasim pacientii acestui doctor
                var patients = await _context.Patients
                    .Where(p => p.DoctorId == doctor.Id)
                    .ToListAsync();

                // 2. Cautam un doctor de inlocuire cu ACEEASI specializare
                Doctor replacement = null;

                if (!string.IsNullOrEmpty(doctor.Specialization))
                {
                    replacement = await _context.Doctors
                        .Where(d => d.Id != doctor.Id
                                 && d.Specialization == doctor.Specialization)
                        .FirstOrDefaultAsync();
                }

                // Fallback: oricare alt doctor daca nu exista cu aceeasi specializare
                if (replacement == null)
                {
                    replacement = await _context.Doctors
                        .Where(d => d.Id != doctor.Id)
                        .FirstOrDefaultAsync();
                }

                // 3. Reatribuim pacientii
                foreach (var p in patients)
                    p.DoctorId = replacement?.Id;

                if (patients.Any())
                    _context.Patients.UpdateRange(patients);

                // 4. Curatam legaturile many-to-many ale doctorului sters
                var pdLinks = await _context.PatientDoctors
                    .Where(pd => pd.DoctorId == doctor.Id)
                    .ToListAsync();
                _context.PatientDoctors.RemoveRange(pdLinks);

                // 5. Adaugam legaturi many-to-many catre doctorul de inlocuire
                if (replacement != null)
                {
                    foreach (var p in patients)
                    {
                        bool alreadyLinked = await _context.PatientDoctors
                            .AnyAsync(pd => pd.PatientId == p.Id && pd.DoctorId == replacement.Id);
                        if (!alreadyLinked)
                        {
                            _context.PatientDoctors.Add(new PatientDoctor
                            {
                                PatientId = p.Id,
                                DoctorId  = replacement.Id
                            });
                        }
                    }
                }

                // 6. Stergem mesajele de chat
                var chatMessages = await _context.ChatMessages
                    .Where(c => c.DoctorId == doctor.Id)
                    .ToListAsync();
                _context.ChatMessages.RemoveRange(chatMessages);

                // 7. Stergem userul asociat doctorului (cascade sterge si Doctor)
                var user = await _context.Users.FindAsync(doctor.UserId);
                if (user != null) _context.Users.Remove(user);

                await _context.SaveChangesAsync();

                string reassignMsg = replacement != null
                    ? $"Patients reassigned to Dr. {replacement.FullName} ({replacement.Specialization ?? "General"})."
                    : "No replacement doctor available. Patients are now unassigned.";

                await Shell.Current.DisplayAlert("Done",
                    $"Dr. {doctor.FullName} deleted.\n{reassignMsg}", "OK");

                await LoadAllData();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        [RelayCommand]
        async Task EditPatient(Patient patient) =>
            await Shell.Current.GoToAsync($"AdminEditPatientPage?patientId={patient.Id}");

        [RelayCommand]
        async Task Logout() => await Shell.Current.GoToAsync("//LoginPage");
    }
}

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

        public AdminDashboardViewModel(AppDbContext context) => _context = context;

        public async Task LoadAllData()
        {
            var patients = await _context.Patients.AsNoTracking().ToListAsync();
            var doctors  = await _context.Doctors.AsNoTracking().ToListAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AllPatients.Clear(); foreach (var p in patients) AllPatients.Add(p);
                AllDoctors.Clear();  foreach (var d in doctors)  AllDoctors.Add(d);
            });
        }

        public async Task LoadAllPatients() => await LoadAllData();

        [RelayCommand]
        async Task DeletePatient(Patient patient)
        {
            bool confirm = await Shell.Current.DisplayAlert("Warning",
                $"Permanently delete {patient.FullName}?", "Yes", "No");
            if (!confirm) return;
            var user = await _context.Users.FindAsync(patient.UserId);
            if (user != null) _context.Users.Remove(user);
            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();
            await LoadAllData();
        }

        [RelayCommand]
        async Task DeleteDoctor(Doctor doctor)
        {
            bool confirm = await Shell.Current.DisplayAlert("Warning",
                $"Delete Dr. {doctor.FullName}? Patients will be reassigned.",
                "Yes, Delete", "Cancel");
            if (!confirm) return;

            try
            {
                var patients = await _context.Patients.Where(p => p.DoctorId == doctor.Id).ToListAsync();

                Doctor replacement = null;
                if (!string.IsNullOrEmpty(doctor.Specialization))
                    replacement = await _context.Doctors
                        .Where(d => d.Id != doctor.Id && d.Specialization == doctor.Specialization)
                        .FirstOrDefaultAsync();
                if (replacement == null)
                    replacement = await _context.Doctors.Where(d => d.Id != doctor.Id).FirstOrDefaultAsync();

                foreach (var p in patients) p.DoctorId = replacement?.Id;
                if (patients.Any()) _context.Patients.UpdateRange(patients);

                var pdLinks = await _context.PatientDoctors.Where(pd => pd.DoctorId == doctor.Id).ToListAsync();
                _context.PatientDoctors.RemoveRange(pdLinks);

                if (replacement != null)
                    foreach (var p in patients)
                    {
                        bool linked = await _context.PatientDoctors.AnyAsync(pd => pd.PatientId == p.Id && pd.DoctorId == replacement.Id);
                        if (!linked) _context.PatientDoctors.Add(new PatientDoctor { PatientId = p.Id, DoctorId = replacement.Id });
                    }

                var chats = await _context.ChatMessages.Where(c => c.DoctorId == doctor.Id).ToListAsync();
                _context.ChatMessages.RemoveRange(chats);

                var user = await _context.Users.FindAsync(doctor.UserId);
                if (user != null) _context.Users.Remove(user);

                await _context.SaveChangesAsync();

                string msg = replacement != null
                    ? $"Patients reassigned to Dr. {replacement.FullName} ({replacement.Specialization ?? "General"})."
                    : "No replacement doctor. Patients are unassigned.";
                await Shell.Current.DisplayAlert("Done", $"Dr. {doctor.FullName} deleted.\n{msg}", "OK");
                await LoadAllData();
            }
            catch (Exception ex) { await Shell.Current.DisplayAlert("Error", ex.Message, "OK"); }
        }

        // Edit patient (existent)
        [RelayCommand]
        async Task EditPatient(Patient patient) =>
            await Shell.Current.GoToAsync($"AdminEditPatientPage?patientId={patient.Id}");

        // Edit doctor — NOU
        [RelayCommand]
        async Task EditDoctor(Doctor doctor) =>
            await Shell.Current.GoToAsync($"AdminEditDoctorPage?doctorId={doctor.Id}");

        [RelayCommand]
        async Task Logout() => await Shell.Current.GoToAsync("//LoginPage");
    }
}

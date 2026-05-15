using _4Life.Data;
using _4Life.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace _4Life.ViewModels
{
    public partial class DoctorDashboardViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        private int _currentDoctorId;
        private List<Patient> _allPatients = new();

        [ObservableProperty] private string searchQuery = string.Empty;
        [ObservableProperty] private int unreadCount;
        [ObservableProperty] private bool hasUnread;

        public ObservableCollection<Patient> MyPatients { get; set; } = new();

        public DoctorDashboardViewModel(AppDbContext context)
        {
            _context = context;
        }

        // ---------------------------------------------------------------
        // INCARCARE DATE
        // ---------------------------------------------------------------
        public async Task LoadPatients(int userId)
        {
            var doctor = await _context.Doctors.AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null) return;

            _currentDoctorId = doctor.Id;

            var patientIdsFromManyToMany = await _context.PatientDoctors
                .Where(pd => pd.DoctorId == doctor.Id)
                .Select(pd => pd.PatientId)
                .ToListAsync();

            _allPatients = await _context.Patients.AsNoTracking()
                .Where(p => p.DoctorId == doctor.Id || patientIdsFromManyToMany.Contains(p.Id))
                .Include(p => p.PrescribedMedicines)
                .ToListAsync();

            MainThread.BeginInvokeOnMainThread(() => ApplySearch());
        }

        // ---------------------------------------------------------------
        // MESAJE NECITITE
        // ---------------------------------------------------------------
        public async Task CheckUnreadMessages(int userId)
        {
            var doctor = await _context.Doctors.AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null) return;

            var lastVisitKey  = $"LastChatVisit_D{doctor.Id}";
            var lastVisitTick = Preferences.Default.Get(lastVisitKey, 0L);
            var lastVisit     = lastVisitTick > 0 ? new DateTime(lastVisitTick) : DateTime.MinValue;

            var unread = await _context.ChatMessages
                .Where(m => m.DoctorId == doctor.Id
                         && m.SenderRole == "Patient"
                         && m.SentAt > lastVisit)
                .ToListAsync();

            UnreadCount = unread.Count;
            HasUnread   = UnreadCount > 0;

            if (HasUnread)
            {
                var lines = new List<string>();
                foreach (var g in unread.GroupBy(m => m.PatientId))
                {
                    var pat = await _context.Patients.FindAsync(g.Key);
                    lines.Add($"{pat?.FullName ?? "Unknown"}: {g.Count()} new message(s)");
                }
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await Shell.Current.DisplayAlert("💬 New Messages from Patients",
                        string.Join("\n", lines), "OK"));
            }
        }

        public void MarkChatVisited(int doctorId)
        {
            Preferences.Default.Set($"LastChatVisit_D{doctorId}", DateTime.Now.Ticks);
            UnreadCount = 0;
            HasUnread   = false;
        }

        // ---------------------------------------------------------------
        // SEARCH
        // ---------------------------------------------------------------
        partial void OnSearchQueryChanged(string value) => ApplySearch();

        private void ApplySearch()
        {
            var query = SearchQuery?.Trim().ToLower() ?? string.Empty;
            var filtered = string.IsNullOrEmpty(query)
                ? _allPatients
                : _allPatients.Where(p => p.FullName.ToLower().Contains(query)).ToList();

            MyPatients.Clear();
            foreach (var p in filtered) MyPatients.Add(p);
        }

        // ---------------------------------------------------------------
        // DELETE MEDICAMENT
        // ---------------------------------------------------------------
        [RelayCommand]
        public async Task DeleteMedicine(Medicine med)
        {
            if (med == null) return;
            bool confirm = await Shell.Current.DisplayAlert("Confirm",
                $"Remove {med.Name} from patient's plan?", "Yes", "No");
            if (!confirm) return;

            try
            {
                var tracked = await _context.Medicines.FindAsync(med.Id);
                if (tracked != null) _context.Medicines.Remove(tracked);
                await _context.SaveChangesAsync();

                int uid = Preferences.Default.Get("CurrentUserId", 0);
                await LoadPatients(uid);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        // ---------------------------------------------------------------
        // SOFT DELETE PACIENT
        // ---------------------------------------------------------------
        [RelayCommand]
        public async Task DeletePatient(Patient patient)
        {
            if (patient == null) return;
            bool confirm = await Shell.Current.DisplayAlert("Remove Patient",
                $"Remove {patient.FullName} from your list?\n\nThey will be reassigned to another doctor of the same specialization.",
                "Yes, Remove", "Cancel");
            if (!confirm) return;

            try
            {
                var currentDoctor = await _context.Doctors.FindAsync(_currentDoctorId);
                if (currentDoctor == null) return;

                var replacement = await _context.Doctors
                    .Where(d => d.Id != _currentDoctorId && d.Specialization == currentDoctor.Specialization)
                    .FirstOrDefaultAsync()
                    ?? await _context.Doctors
                    .Where(d => d.Id != _currentDoctorId)
                    .FirstOrDefaultAsync();

                var trackedPatient = await _context.Patients.FindAsync(patient.Id);
                if (trackedPatient != null)
                {
                    if (trackedPatient.DoctorId == _currentDoctorId)
                        trackedPatient.DoctorId = replacement?.Id;
                    _context.Patients.Update(trackedPatient);
                }

                var link = await _context.PatientDoctors
                    .FirstOrDefaultAsync(pd => pd.PatientId == patient.Id && pd.DoctorId == _currentDoctorId);
                if (link != null) _context.PatientDoctors.Remove(link);

                if (replacement != null)
                {
                    bool alreadyLinked = await _context.PatientDoctors
                        .AnyAsync(pd => pd.PatientId == patient.Id && pd.DoctorId == replacement.Id);
                    if (!alreadyLinked)
                        _context.PatientDoctors.Add(new PatientDoctor { PatientId = patient.Id, DoctorId = replacement.Id });
                }

                await _context.SaveChangesAsync();

                string msg = replacement != null
                    ? $"{patient.FullName} reassigned to Dr. {replacement.FullName}."
                    : $"{patient.FullName} removed. No replacement found.";
                await Shell.Current.DisplayAlert("Done", msg, "OK");

                int uid = Preferences.Default.Get("CurrentUserId", 0);
                await LoadPatients(uid);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        // ---------------------------------------------------------------
        // JOURNAL PACIENT — NOU
        // ---------------------------------------------------------------
        [RelayCommand]
        async Task ViewPatientJournal(Patient patient)
        {
            if (patient == null) return;
            await Shell.Current.GoToAsync($"PatientJournalPage?patientId={patient.Id}");
        }

        // ---------------------------------------------------------------
        // CHAT / PRESCRIPTII / EMERGENTE / LOGOUT
        // ---------------------------------------------------------------
        [RelayCommand]
        async Task OpenChat(Patient patient)
        {
            if (patient == null) return;
            MarkChatVisited(_currentDoctorId);
            await Shell.Current.GoToAsync(
                $"ChatPage?patientId={patient.Id}&doctorId={_currentDoctorId}&senderRole=Doctor");
        }

        [RelayCommand]
        async Task GoToPrescribe(Patient patient) =>
            await Shell.Current.GoToAsync($"PrescribePage?patientId={patient.Id}");

        [RelayCommand]
        async Task GoToEditPrescription(Medicine med) =>
            await Shell.Current.GoToAsync($"PrescribePage?medicineId={med.Id}");

        public async Task CheckForEmergencies(int userId)
        {
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId);
            if (doctor == null) return;

            var crisis = await _context.Patients.AsNoTracking()
                .FirstOrDefaultAsync(p => p.DoctorId == doctor.Id && p.hasActiveAlert == true);

            if (crisis != null)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await Shell.Current.DisplayAlert("🚨 EMERGENCY",
                        $"Patient: {crisis.FullName}\n\n{crisis.emergencyMessage}", "OK"));

                var toUpdate = await _context.Patients.FindAsync(crisis.Id);
                if (toUpdate != null)
                {
                    toUpdate.hasActiveAlert   = false;
                    toUpdate.emergencyMessage = string.Empty;
                    await _context.SaveChangesAsync();
                }
            }
        }

        [RelayCommand]
        async Task Logout()
        {
            bool answer = await Shell.Current.DisplayAlert("Logout",
                "Are you sure you want to logout?", "Yes", "No");
            if (answer)
            {
                Preferences.Default.Remove("CurrentUserId");
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }
    }
}

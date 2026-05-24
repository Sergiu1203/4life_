using _4Life.Data;
using _4Life.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace _4Life.ViewModels
{
    [QueryProperty(nameof(UserName), "name")]
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        private int _currentPatientId;
        private int _currentDoctorId;
        private List<Medicine> _allMeds = new();

        [ObservableProperty] private string userName;
        [ObservableProperty] private string searchQuery      = string.Empty;
        [ObservableProperty] private int    unreadCount;
        [ObservableProperty] private bool   hasUnread;
        [ObservableProperty] private bool   doctorPanelOpen  = false;
        [ObservableProperty] private string doctorPanelArrow = "▲";

        partial void OnDoctorPanelOpenChanged(bool value) => DoctorPanelArrow = value ? "▼" : "▲";
        [RelayCommand] void ToggleDoctorPanel() => DoctorPanelOpen = !DoctorPanelOpen;

        public ObservableCollection<MedicineEntry> DailyMeds { get; set; } = new();
        public ObservableCollection<Doctor> MyDoctors { get; set; } = new();

        public DashboardViewModel(AppDbContext context) { _context = context; }

        // ---------------------------------------------------------------
        // INCARCARE DATE
        // ---------------------------------------------------------------
        public async Task LoadPatientData(int userId)
        {
            var patient = await _context.Patients
                .Include(p => p.PatientDoctors).ThenInclude(pd => pd.Doctor)
                .FirstOrDefaultAsync(p => p.UserId == userId);
            if (patient == null) return;

            _currentPatientId = patient.Id;
            _currentDoctorId  = patient.DoctorId ?? 0;

            _allMeds = await _context.Medicines.Where(m => m.PatientId == patient.Id).ToListAsync();

            var doctors = patient.PatientDoctors.Select(pd => pd.Doctor).ToList();
            if (!doctors.Any() && patient.DoctorId.HasValue)
            {
                var d = await _context.Doctors.FindAsync(patient.DoctorId.Value);
                if (d != null) doctors.Add(d);
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MyDoctors.Clear();
                foreach (var d in doctors) MyDoctors.Add(d);
                ApplySearch();
            });

            await CheckLowStock(_allMeds);
            await CheckUnreadMessages();
        }

        // ---------------------------------------------------------------
        // EXTINDE IN INTRARI PER MOMENT AL ZILEI
        // ---------------------------------------------------------------
        private List<MedicineEntry> ExpandToEntries(List<Medicine> meds)
        {
            var entries = new List<MedicineEntry>();
            foreach (var med in meds)
            {
                var times = new List<string>();
                if (!string.IsNullOrEmpty(med.MealTimes))
                    times = med.MealTimes.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
                else if (!string.IsNullOrEmpty(med.TimeOfDay))
                    times.Add(med.TimeOfDay);
                else
                    times.Add("General");

                foreach (var time in times)
                {
                    var entry = new MedicineEntry
                    {
                        Source   = med,
                        Name     = med.Name,
                        MealTime = time,
                        IsOtc    = med.PrescribedByDoctorId == null
                    };

                    // Restaureaza starea din Preferences (UI local)
                    // DAR validam si fata de starea din DB (per moment)
                    bool prefState = Preferences.Default.Get(entry.PreferenceKey, false);
                    bool dbState   = med.GetIsTakenForTime(time);

                    // Folosim starea DB ca sursa de adevar — Preferences e doar cache UI
                    entry.IsTaken = dbState;
                    Preferences.Default.Set(entry.PreferenceKey, dbState);

                    entries.Add(entry);
                }
            }
            return entries;
        }

        // ---------------------------------------------------------------
        // SEARCH
        // ---------------------------------------------------------------
        partial void OnSearchQueryChanged(string value) => ApplySearch();

        private void ApplySearch()
        {
            var q = SearchQuery?.Trim().ToLower() ?? string.Empty;
            var filtered = string.IsNullOrEmpty(q)
                ? _allMeds
                : _allMeds.Where(m =>
                    m.Name.ToLower().Contains(q) ||
                    (m.Category?.ToLower().Contains(q) ?? false)).ToList();

            DailyMeds.Clear();
            foreach (var e in ExpandToEntries(filtered)) DailyMeds.Add(e);
        }

        // ---------------------------------------------------------------
        // TOGGLE MEDICATION — decrement corect per moment al zilei
        //
        // Logica:
        // - Fiecare MedicineEntry are MealTime propriu ("Morning", "Lunch" etc.)
        // - Medicine are MorningTaken / LunchTaken / DinnerTaken in DB (independent)
        // - Comparăm entry.IsTaken cu dbMed.GetIsTakenForTime(entry.MealTime)
        //   => detectam daca e o schimbare reala sau o restaurare la incarcare
        // - Stock se decrementeaza cu entry.Dose (doza specifica momentului)
        // ---------------------------------------------------------------
        [RelayCommand]
        public async Task ToggleMedicationTaken(MedicineEntry entry)
        {
            if (entry == null) return;

            // Citim starea curenta din DB
            var dbMed = await _context.Medicines.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == entry.Source.Id);
            if (dbMed == null) return;

            // Starea per moment din DB pentru acest MealTime
            bool dbTakenForThisTime = dbMed.GetIsTakenForTime(entry.MealTime);

            // Daca starea UI == starea DB pentru ACEST moment => restaurare, nu actiune
            if (entry.IsTaken == dbTakenForThisTime)
            {
                Preferences.Default.Set(entry.PreferenceKey, entry.IsTaken);
                return;
            }

            // Doza pentru acest moment
            int dose     = entry.Dose;
            int newStock = dbMed.StockQuantity;

            if (entry.IsTaken)
            {
                // Pacientul tocmai a bifat — decrementeaza cu doza acestui moment
                if (newStock >= dose)
                {
                    newStock -= dose;
                    if (newStock < 5)
                        await Shell.Current.DisplayAlert("⚠️ Low Stock",
                            $"{entry.Name} has only {newStock} units left!", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Out of Stock",
                        $"Not enough {entry.Name}! Only {newStock} left.", "OK");
                    entry.IsTaken = false;
                    Preferences.Default.Set(entry.PreferenceKey, false);
                    return;
                }
            }
            else
            {
                // Pacientul a debifat — restaureaza doza acestui moment
                newStock += dose;
            }

            // Salveaza in DB: stocul nou + IsTaken per moment
            var tracked = await _context.Medicines.FindAsync(entry.Source.Id);
            if (tracked != null)
            {
                tracked.StockQuantity = newStock;
                tracked.SetIsTakenForTime(entry.MealTime, entry.IsTaken);

                // Actualizam si Source (obiectul in memorie) pentru UI refresh imediat
                entry.Source.StockQuantity = newStock;
                entry.Source.SetIsTakenForTime(entry.MealTime, entry.IsTaken);

                await _context.SaveChangesAsync();
            }

            // Salveaza starea in Preferences
            Preferences.Default.Set(entry.PreferenceKey, entry.IsTaken);

            // Notifica toate intrarile din acelasi medicament sa-si actualizeze stocul
            MainThread.BeginInvokeOnMainThread(() =>
            {
                foreach (var e in DailyMeds.Where(e => e.Source.Id == entry.Source.Id))
                    e.NotifyStockChanged();
            });
        }

        // ---------------------------------------------------------------
        // STERGE MEDICAMENT PROPRIU
        // ---------------------------------------------------------------
        [RelayCommand]
        public async Task DeleteOwnMedicine(MedicineEntry entry)
        {
            if (entry == null || !entry.IsOtc) return;
            bool confirm = await Shell.Current.DisplayAlert("Remove",
                $"Remove {entry.Name} from your schedule?", "Yes", "Cancel");
            if (!confirm) return;

            var medId = entry.Source.Id;
            var dbMed = await _context.Medicines.FindAsync(medId);
            if (dbMed != null) { _context.Medicines.Remove(dbMed); await _context.SaveChangesAsync(); }

            _allMeds.RemoveAll(m => m.Id == medId);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var toRemove = DailyMeds.Where(e => e.Source.Id == medId).ToList();
                foreach (var e in toRemove) DailyMeds.Remove(e);
            });
        }

        // ---------------------------------------------------------------
        // STOC MIC
        // ---------------------------------------------------------------
        private async Task CheckLowStock(List<Medicine> meds)
        {
            var low = meds.Where(m => m.StockQuantity > 0 && m.StockQuantity < 5).ToList();
            if (!low.Any()) return;
            var names = string.Join("\n", low.Select(m => $"• {m.Name}: {m.StockQuantity} left"));
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await Shell.Current.DisplayAlert("⚠️ Low Stock Warning",
                    $"Running low:\n\n{names}\n\nContact your doctor.", "OK"));
        }

        // ---------------------------------------------------------------
        // MESAJE NECITITE
        // ---------------------------------------------------------------
        public async Task CheckUnreadMessages()
        {
            if (_currentPatientId == 0) return;
            var key  = $"LastChatVisit_P{_currentPatientId}";
            var tick = Preferences.Default.Get(key, 0L);
            var last = tick > 0 ? new DateTime(tick) : DateTime.MinValue;

            var unread = await _context.ChatMessages
                .Where(m => m.PatientId == _currentPatientId && m.SenderRole == "Doctor" && m.SentAt > last)
                .ToListAsync();

            UnreadCount = unread.Count;
            HasUnread   = UnreadCount > 0;

            if (HasUnread)
            {
                var lines = new List<string>();
                foreach (var g in unread.GroupBy(m => m.DoctorId))
                {
                    var doc = await _context.Doctors.FindAsync(g.Key);
                    lines.Add($"Dr. {doc?.FullName ?? "Unknown"}: {g.Count()} new message(s)");
                }
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await Shell.Current.DisplayAlert("💬 New Messages", string.Join("\n", lines), "OK"));
            }
        }

        public void MarkChatVisited()
        {
            Preferences.Default.Set($"LastChatVisit_P{_currentPatientId}", DateTime.Now.Ticks);
            UnreadCount = 0; HasUnread = false;
        }

        // ---------------------------------------------------------------
        // NAVIGARE
        // ---------------------------------------------------------------
        [RelayCommand]
        private async Task OpenChat(Doctor doctor)
        {
            if (doctor == null) return;
            MarkChatVisited();
            await Shell.Current.GoToAsync(
                $"ChatPage?patientId={_currentPatientId}&doctorId={doctor.Id}&senderRole=Patient");
        }

        [RelayCommand]
        private async Task ManageDoctors()
            => await Shell.Current.GoToAsync($"SelectDoctorsPage?patientId={_currentPatientId}");

        [RelayCommand]
        private async Task AddOwnMedicine()
            => await Shell.Current.GoToAsync(nameof(_4Life.Views.AddOwnMedicinePage));

        [RelayCommand]
        private async Task SosContact()
        {
            bool confirm = await Shell.Current.DisplayAlert("🚨 Emergency Call",
                "This will call emergency services (112). Continue?", "Call 112", "Cancel");
            if (confirm && PhoneDialer.Default.IsSupported)
                PhoneDialer.Default.Open("112");
        }

        [RelayCommand] async Task GoToJournal() => await Shell.Current.GoToAsync("SymptomJournalPage");

        [RelayCommand]
        async Task Logout()
        {
            bool answer = await Shell.Current.DisplayAlert("Logout", "Are you sure?", "Yes", "No");
            if (answer) { Preferences.Default.Remove("CurrentUserId"); await Shell.Current.GoToAsync("//LoginPage"); }
        }
    }
}

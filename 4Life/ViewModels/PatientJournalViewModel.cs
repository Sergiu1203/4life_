using _4Life.Data;
using _4Life.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace _4Life.ViewModels
{
    [QueryProperty(nameof(PatientId), "patientId")]
    public partial class PatientJournalViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty] private int patientId;
        [ObservableProperty] private string patientName     = string.Empty;
        [ObservableProperty] private string manualNotes     = string.Empty;
        [ObservableProperty] private string checkInHistory  = string.Empty;
        [ObservableProperty] private bool   hasManualNotes;
        [ObservableProperty] private bool   hasCheckInHistory;
        [ObservableProperty] private bool   hasAlertHistory;
        [ObservableProperty] private string alertHistory    = string.Empty;

        // Medicamente luate azi de acest pacient
        // Doctorul nu are acces la Preferences ale pacientului (alt device),
        // deci afisam medicamentele marcate ca IsTaken=true in DB
        public ObservableCollection<TakenMedEntry> TakenMedsToday { get; set; } = new();

        public PatientJournalViewModel(AppDbContext context)
        {
            _context = context;
        }

        // Se apeleaza din OnAppearing dupa ce QueryProperty s-a setat
        public async Task LoadData()
        {
            if (PatientId == 0) return;

            var patient = await _context.Patients
                .Include(p => p.PrescribedMedicines)
                .FirstOrDefaultAsync(p => p.Id == PatientId);

            if (patient == null) return;

            PatientName = patient.FullName;

            // -------------------------------------------------------
            // SEPARAM notele manuale de check-in-urile automate
            // -------------------------------------------------------
            if (!string.IsNullOrEmpty(patient.JournalNotes))
            {
                var allEntries = patient.JournalNotes
                    .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                // Note manuale
                var manual = allEntries
                    .Where(e => !e.TrimStart().StartsWith("[Daily Check-in"))
                    .ToList();
                ManualNotes    = string.Join("\n\n", manual);
                HasManualNotes = manual.Any();

                // Check-in-uri automate — cele mai recente primele
                var checkIns = allEntries
                    .Where(e => e.TrimStart().StartsWith("[Daily Check-in"))
                    .Reverse()
                    .Take(14) // ultimele 2 saptamani
                    .ToList();
                CheckInHistory    = string.Join("\n\n───────────────\n\n", checkIns);
                HasCheckInHistory = checkIns.Any();
            }

            // -------------------------------------------------------
            // MEDICAMENTE LUATE AZI — IsTaken din DB
            // -------------------------------------------------------
            TakenMedsToday.Clear();

            foreach (var med in patient.PrescribedMedicines)
            {
                if (!med.IsTaken) continue;

                // Daca are MealTimes, le afisam toate ca intrari separate
                var times = new List<string>();
                if (!string.IsNullOrEmpty(med.MealTimes))
                    times = med.MealTimes.Split(',').Select(t => t.Trim())
                                         .Where(t => !string.IsNullOrEmpty(t)).ToList();
                else if (!string.IsNullOrEmpty(med.TimeOfDay))
                    times.Add(med.TimeOfDay);
                else
                    times.Add("General");

                foreach (var time in times)
                {
                    TakenMedsToday.Add(new TakenMedEntry
                    {
                        Name      = med.Name,
                        Dosage    = med.Dosage,
                        MealTime  = time,
                        MealEmoji = time switch
                        {
                            "Morning" => "🌅",
                            "Lunch"   => "☀️",
                            "Dinner"  => "🌙",
                            _         => "⏰"
                        }
                    });
                }
            }

            // -------------------------------------------------------
            // ALERTE TRIMISE DE PACIENT (istoricul mesajelor de urgenta)
            // -------------------------------------------------------
            var alerts = await _context.ChatMessages
                .Where(m => m.PatientId == PatientId && m.SenderRole == "Patient")
                .OrderByDescending(m => m.SentAt)
                .Take(10)
                .ToListAsync();

            if (alerts.Any())
            {
                AlertHistory    = string.Join("\n\n",
                    alerts.Select(a => $"[{a.SentAt:dd MMM HH:mm}] {a.Content}"));
                HasAlertHistory = true;
            }
        }

        [RelayCommand]
        async Task GoBack() => await Shell.Current.GoToAsync("..");
    }
}

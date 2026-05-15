using _4Life.Data;
using _4Life.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace _4Life.ViewModels
{
    public partial class SymptomJournalViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        // Notele MANUALE ale pacientului — stocate in Patient.ManualNotes (camp nou)
        // Fallback: daca ManualNotes e null, afisam doar ce nu e check-in din JournalNotes
        [ObservableProperty] private string journalText;

        // Istoricul check-in-urilor automate
        [ObservableProperty] private string checkInHistory = string.Empty;
        [ObservableProperty] private bool hasCheckInHistory;

        // Medicamente luate azi
        public ObservableCollection<TakenMedEntry> TakenMedsToday { get; set; } = new();

        public SymptomJournalViewModel(AppDbContext context)
        {
            _context = context;
        }

        public async Task LoadJournalData()
        {
            int userId  = Preferences.Default.Get("CurrentUserId", 0);
            var patient = await _context.Patients
                .Include(p => p.PrescribedMedicines)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient == null) return;

            // -------------------------------------------------------
            // NOTE MANUALE — tot ce NU e un check-in automat
            // Check-in-urile automate incep cu "[Daily Check-in"
            // -------------------------------------------------------
            if (!string.IsNullOrEmpty(patient.JournalNotes))
            {
                var allEntries = patient.JournalNotes
                    .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                // Note manuale = tot ce nu e check-in automat
                var manualEntries = allEntries
                    .Where(e => !e.TrimStart().StartsWith("[Daily Check-in"))
                    .ToList();

                JournalText = string.Join("\n\n", manualEntries);

                // Check-in-uri automate — cele mai recente primele, max 10
                var checkIns = allEntries
                    .Where(e => e.TrimStart().StartsWith("[Daily Check-in"))
                    .Reverse()
                    .Take(10)
                    .ToList();

                CheckInHistory    = string.Join("\n\n───────────────\n\n", checkIns);
                HasCheckInHistory = checkIns.Any();
            }
            else
            {
                JournalText       = string.Empty;
                CheckInHistory    = string.Empty;
                HasCheckInHistory = false;
            }

            // -------------------------------------------------------
            // MEDICAMENTE LUATE AZI — din Preferences
            // -------------------------------------------------------
            TakenMedsToday.Clear();

            foreach (var med in patient.PrescribedMedicines)
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
                    string prefKey = $"Taken_{med.Id}_{time}_{DateTime.Today:yyyy-MM-dd}";
                    bool   taken   = Preferences.Default.Get(prefKey, false);

                    if (taken)
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
            }
        }

        // -------------------------------------------------------
        // SALVEAZA NOTE MANUALE
        // Pastreaza check-in-urile automate existente si inlocuieste
        // doar sectiunea de note manuale
        // -------------------------------------------------------
        [RelayCommand]
        async Task SaveJournal()
        {
            int userId  = Preferences.Default.Get("CurrentUserId", 0);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient == null) return;

            // Extragem check-in-urile existente din DB
            var existingCheckIns = new List<string>();
            if (!string.IsNullOrEmpty(patient.JournalNotes))
            {
                existingCheckIns = patient.JournalNotes
                    .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
                    .Where(e => e.TrimStart().StartsWith("[Daily Check-in"))
                    .ToList();
            }

            // Reconstruim JournalNotes: note manuale + check-in-uri (la final)
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(JournalText))
                parts.Add(JournalText.Trim());

            parts.AddRange(existingCheckIns);

            patient.JournalNotes = string.Join("\n\n", parts);
            _context.Patients.Update(patient);
            await _context.SaveChangesAsync();

            await Shell.Current.DisplayAlert("Saved", "Notes saved!", "OK");
        }
    }

    public class TakenMedEntry
    {
        public string Name      { get; set; }
        public string Dosage    { get; set; }
        public string MealTime  { get; set; }
        public string MealEmoji { get; set; }
    }
}

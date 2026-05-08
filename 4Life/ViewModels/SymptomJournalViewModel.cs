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
    public partial class SymptomJournalViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty] private string journalText;
        public ObservableCollection<Medicine> TakenMedsHistory { get; set; } = new();

        public SymptomJournalViewModel(AppDbContext context)
        {
            _context = context;
        }

        public async Task LoadJournalData()
        {
            int userId = Preferences.Default.Get("CurrentUserId", 0);
            var patient = await _context.Patients
                .Include(p => p.PrescribedMedicines)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient != null)
            {
                JournalText = patient.JournalNotes;

                TakenMedsHistory.Clear();
                var taken = patient.PrescribedMedicines.Where(m => m.IsTaken).ToList();
                foreach (var m in taken) TakenMedsHistory.Add(m);
            }
        }

        [RelayCommand]
        async Task SaveJournal()
        {
            int userId = Preferences.Default.Get("CurrentUserId", 0);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient != null)
            {
                patient.JournalNotes = JournalText;
                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();
                await Shell.Current.DisplayAlert("Saved", "Journal updated successfully!", "OK");
            }
        }
    }
}

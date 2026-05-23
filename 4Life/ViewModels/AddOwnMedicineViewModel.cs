using _4Life.Data;
using _4Life.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace _4Life.ViewModels
{
    public partial class AddOwnMedicineViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        private bool _suppressSearch = false;

        [ObservableProperty] private string medName      = string.Empty;
        [ObservableProperty] private string initialStock = string.Empty;
        [ObservableProperty] private bool   takeMorning;
        [ObservableProperty] private bool   takeLunch;
        [ObservableProperty] private bool   takeDinner;
        [ObservableProperty] private string morningDose  = string.Empty;
        [ObservableProperty] private string lunchDose    = string.Empty;
        [ObservableProperty] private string dinnerDose   = string.Empty;
        [ObservableProperty] private string timesError   = string.Empty;
        [ObservableProperty] private bool   showSuggestions = false;

        public ObservableCollection<MedicineSuggestion> FilteredSuggestions { get; } = new();

        public AddOwnMedicineViewModel(AppDbContext context) { _context = context; }

        partial void OnMedNameChanged(string value)
        {
            if (_suppressSearch) return;
            FilteredSuggestions.Clear();
            if (string.IsNullOrWhiteSpace(value)) { ShowSuggestions = false; return; }
            var matches = MedicineDatabase.All
                .Where(m => m.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
                .Take(8).ToList();
            foreach (var m in matches) FilteredSuggestions.Add(m);
            ShowSuggestions = FilteredSuggestions.Any();
        }

        [RelayCommand]
        void SelectSuggestion(MedicineSuggestion s)
        {
            if (s == null) return;
            _suppressSearch = true;
            MedName = s.Name;
            _suppressSearch = false;
            FilteredSuggestions.Clear();
            ShowSuggestions = false;
        }

        [RelayCommand] void DismissSuggestions() => ShowSuggestions = false;

        private string BuildMealTimes()
        {
            var parts = new List<string>();
            if (TakeMorning) parts.Add("Morning");
            if (TakeLunch)   parts.Add("Lunch");
            if (TakeDinner)  parts.Add("Dinner");
            return string.Join(",", parts);
        }

        [RelayCommand]
        async Task Save()
        {
            if (string.IsNullOrWhiteSpace(MedName))
            {
                await Shell.Current.DisplayAlert("Error", "Please enter a medicine name.", "OK");
                return;
            }

            var mealTimes = BuildMealTimes();
            if (string.IsNullOrEmpty(mealTimes)) { TimesError = "Please select at least one time of day."; return; }
            TimesError = string.Empty;

            if (TakeMorning && (!int.TryParse(MorningDose, out int m) || m < 1))
            {
                await Shell.Current.DisplayAlert("Error", "Please enter pills for Morning (min 1).", "OK"); return;
            }
            if (TakeLunch && (!int.TryParse(LunchDose, out int l) || l < 1))
            {
                await Shell.Current.DisplayAlert("Error", "Please enter pills for Lunch (min 1).", "OK"); return;
            }
            if (TakeDinner && (!int.TryParse(DinnerDose, out int d) || d < 1))
            {
                await Shell.Current.DisplayAlert("Error", "Please enter pills for Dinner (min 1).", "OK"); return;
            }

            int morningPills = TakeMorning ? int.Parse(MorningDose) : 0;
            int lunchPills   = TakeLunch   ? int.Parse(LunchDose)   : 0;
            int dinnerPills  = TakeDinner  ? int.Parse(DinnerDose)  : 0;

            var parts = new List<string>();
            if (morningPills > 0) parts.Add($"Morning: {morningPills}");
            if (lunchPills   > 0) parts.Add($"Lunch: {lunchPills}");
            if (dinnerPills  > 0) parts.Add($"Dinner: {dinnerPills}");
            string dosageSummary = string.Join(" | ", parts);

            int currentUserId = Preferences.Default.Get("CurrentUserId", 0);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == currentUserId);
            if (patient == null) { await Shell.Current.DisplayAlert("Error", "Patient not found.", "OK"); return; }

            var suggestion = MedicineDatabase.FindByName(MedName);
            var med = new Medicine
            {
                Name                 = MedName.Trim(),
                Dosage               = dosageSummary,
                MealTimes            = mealTimes,
                TimeOfDay            = mealTimes,
                MorningDose          = morningPills,
                LunchDose            = lunchPills,
                DinnerDose           = dinnerPills,
                PatientId            = patient.Id,
                PrescribedByDoctorId = null,
                Category             = suggestion?.Category ?? "OTC",
                StockQuantity        = int.TryParse(InitialStock, out int s) && s > 0 ? s : 30,
                IsTaken              = false,
                TargetDate           = DateTime.Today,
                ExpiryDate           = DateTime.Today.AddYears(1)
            };

            _context.Medicines.Add(med);
            await _context.SaveChangesAsync();
            await Shell.Current.DisplayAlert("Added", $"{med.Name} added.", "OK");
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand] async Task Cancel() => await Shell.Current.GoToAsync("..");
    }
}

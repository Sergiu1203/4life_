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
        [ObservableProperty] private string dosage       = string.Empty;
        [ObservableProperty] private string initialStock = string.Empty;  // gol implicit
        [ObservableProperty] private bool takeMorning;
        [ObservableProperty] private bool takeLunch;
        [ObservableProperty] private bool takeDinner;
        [ObservableProperty] private string timesError   = string.Empty;

        // Dropdown search
        [ObservableProperty] private bool showSuggestions = false;
        public ObservableCollection<MedicineSuggestion> FilteredSuggestions { get; } = new();

        public AddOwnMedicineViewModel(AppDbContext context)
        {
            _context = context;
        }

        partial void OnMedNameChanged(string value)
        {
            if (_suppressSearch) return;

            FilteredSuggestions.Clear();

            if (string.IsNullOrWhiteSpace(value))
            {
                ShowSuggestions = false;
                return;
            }

            var matches = MedicineDatabase.All
                .Where(m => m.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
                .Take(8)
                .ToList();

            foreach (var m in matches)
                FilteredSuggestions.Add(m);

            ShowSuggestions = FilteredSuggestions.Any();
        }

        [RelayCommand]
        void SelectSuggestion(MedicineSuggestion suggestion)
        {
            if (suggestion == null) return;

            _suppressSearch = true;
            MedName  = suggestion.Name;
            Dosage   = suggestion.SuggestedDosage;
            _suppressSearch = false;

            FilteredSuggestions.Clear();
            ShowSuggestions = false;
        }

        [RelayCommand]
        void DismissSuggestions() => ShowSuggestions = false;

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
            if (string.IsNullOrEmpty(mealTimes))
            {
                TimesError = "Please select at least one time of day.";
                return;
            }
            TimesError = string.Empty;

            int stock = int.TryParse(InitialStock, out int s) && s > 0 ? s : 30;

            int currentUserId = Preferences.Default.Get("CurrentUserId", 0);
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == currentUserId);

            if (patient == null)
            {
                await Shell.Current.DisplayAlert("Error", "Patient not found.", "OK");
                return;
            }

            var suggestion = MedicineDatabase.FindByName(MedName);

            var med = new Medicine
            {
                Name                 = MedName.Trim(),
                Dosage               = string.IsNullOrWhiteSpace(Dosage) ? "As needed" : Dosage.Trim(),
                MealTimes            = mealTimes,
                TimeOfDay            = mealTimes,
                PatientId            = patient.Id,
                PrescribedByDoctorId = null,
                Category             = suggestion?.Category ?? "OTC",
                StockQuantity        = stock,
                IsTaken              = false,
                TargetDate           = DateTime.Today,
                ExpiryDate           = DateTime.Today.AddYears(1)
            };

            _context.Medicines.Add(med);
            await _context.SaveChangesAsync();

            await Shell.Current.DisplayAlert("Added", $"{med.Name} added to your schedule.", "OK");
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        async Task Cancel() => await Shell.Current.GoToAsync("..");
    }
}

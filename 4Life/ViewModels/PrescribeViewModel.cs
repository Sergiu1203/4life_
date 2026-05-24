using _4Life.Data;
using _4Life.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace _4Life.ViewModels
{
    [QueryProperty(nameof(PatientId),  "patientId")]
    [QueryProperty(nameof(MedicineId), "medicineId")]
    public partial class PrescribeViewModel : ObservableObject
    {
        private readonly AppDbContext _context;
        private bool _suppressSearch = false;

        [ObservableProperty] private int    medicineId;
        [ObservableProperty] private string pageTitle     = "New Prescription";
        [ObservableProperty] private int    patientId;
        [ObservableProperty] private string medName       = string.Empty;
        [ObservableProperty] private string initialStock  = string.Empty;
        [ObservableProperty] private string timesError    = string.Empty;

        // Selectia momentelor zilei
        [ObservableProperty] private bool takeMorning;
        [ObservableProperty] private bool takeLunch;
        [ObservableProperty] private bool takeDinner;

        // Doza per moment (numar de pastile)
        [ObservableProperty] private string morningDose = string.Empty;
        [ObservableProperty] private string lunchDose   = string.Empty;
        [ObservableProperty] private string dinnerDose  = string.Empty;

        // Dropdown search
        [ObservableProperty] private bool showSuggestions = false;
        public ObservableCollection<MedicineSuggestion> FilteredSuggestions { get; } = new();

        public PrescribeViewModel(AppDbContext context) { _context = context; }

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

        partial void OnMedicineIdChanged(int value) { if (value > 0) LoadExistingMedicine(value); }

        private async void LoadExistingMedicine(int id)
        {
            var med = await _context.Medicines.FindAsync(id);
            if (med == null) return;

            _suppressSearch = true;
            PageTitle    = "Edit Prescription";
            MedName      = med.Name;
            InitialStock = med.StockQuantity.ToString();
            PatientId    = med.PatientId;
            _suppressSearch = false;

            var times = med.MealTimes ?? med.TimeOfDay ?? string.Empty;
            TakeMorning = times.Contains("Morning");
            TakeLunch   = times.Contains("Lunch");
            TakeDinner  = times.Contains("Dinner");

            // Incarca dozele salvate
            if (med.MorningDose > 0) MorningDose = med.MorningDose.ToString();
            if (med.LunchDose   > 0) LunchDose   = med.LunchDose.ToString();
            if (med.DinnerDose  > 0) DinnerDose  = med.DinnerDose.ToString();
        }

        private string BuildMealTimes()
        {
            var parts = new List<string>();
            if (TakeMorning) parts.Add("Morning");
            if (TakeLunch)   parts.Add("Lunch");
            if (TakeDinner)  parts.Add("Dinner");
            return string.Join(",", parts);
        }

        [RelayCommand]
        private async Task SavePrescription()
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

            // Valideaza ca fiecare moment selectat are o doza valida (>= 1)
            if (TakeMorning && (!int.TryParse(MorningDose, out int md) || md < 1))
            {
                await Shell.Current.DisplayAlert("Error", "Please enter pills for Morning (min 1).", "OK");
                return;
            }
            if (TakeLunch && (!int.TryParse(LunchDose, out int ld) || ld < 1))
            {
                await Shell.Current.DisplayAlert("Error", "Please enter pills for Lunch (min 1).", "OK");
                return;
            }
            if (TakeDinner && (!int.TryParse(DinnerDose, out int dd) || dd < 1))
            {
                await Shell.Current.DisplayAlert("Error", "Please enter pills for Dinner (min 1).", "OK");
                return;
            }

            int morningPills = TakeMorning ? int.Parse(MorningDose) : 0;
            int lunchPills   = TakeLunch   ? int.Parse(LunchDose)   : 0;
            int dinnerPills  = TakeDinner  ? int.Parse(DinnerDose)  : 0;
            int totalPerDay  = morningPills + lunchPills + dinnerPills;

            // Doza afisata = rezumatul complet
            string dosageSummary = BuildDosageSummary(morningPills, lunchPills, dinnerPills);

            int stock = int.TryParse(InitialStock, out int s) && s > 0 ? s : 30;

            try
            {
                int currentUserId = Preferences.Default.Get("CurrentUserId", 0);
                var doctor    = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == currentUserId);
                var suggestion = MedicineDatabase.FindByName(MedName);

                Medicine medToSave;
                if (MedicineId > 0)
                {
                    medToSave = await _context.Medicines.FindAsync(MedicineId);
                    if (medToSave != null)
                    {
                        medToSave.Name          = MedName.Trim();
                        medToSave.Dosage        = dosageSummary;
                        medToSave.MealTimes     = mealTimes;
                        medToSave.TimeOfDay     = mealTimes;
                        medToSave.MorningDose   = morningPills;
                        medToSave.LunchDose     = lunchPills;
                        medToSave.DinnerDose    = dinnerPills;
                        medToSave.Category      = suggestion?.Category ?? medToSave.Category;
                        medToSave.StockQuantity = stock;
                    }
                }
                else
                {
                    medToSave = new Medicine
                    {
                        Name                 = MedName.Trim(),
                        Dosage               = dosageSummary,
                        MealTimes            = mealTimes,
                        TimeOfDay            = mealTimes,
                        MorningDose          = morningPills,
                        LunchDose            = lunchPills,
                        DinnerDose           = dinnerPills,
                        Category             = suggestion?.Category ?? "Prescription",
                        PatientId            = PatientId,
                        PrescribedByDoctorId = doctor?.Id,
                        TargetDate           = DateTime.Today,
                        StockQuantity        = stock,
                        IsTaken              = false,
                        ExpiryDate           = DateTime.Today.AddYears(1)
                    };
                    _context.Medicines.Add(medToSave);
                }

                await _context.SaveChangesAsync();
                await Shell.Current.DisplayAlert("Success", "Prescription saved!", "OK");
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.InnerException?.Message ?? ex.Message, "OK");
            }
        }

        private string BuildDosageSummary(int morning, int lunch, int dinner)
        {
            var parts = new List<string>();
            if (morning > 0) parts.Add($"Morning: {morning}");
            if (lunch   > 0) parts.Add($"Lunch: {lunch}");
            if (dinner  > 0) parts.Add($"Dinner: {dinner}");
            return string.Join(" | ", parts);
        }
    }
}

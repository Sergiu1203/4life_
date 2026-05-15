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

        [ObservableProperty] private int medicineId;
        [ObservableProperty] private string pageTitle    = "New Prescription";
        [ObservableProperty] private int patientId;
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

        public PrescribeViewModel(AppDbContext context)
        {
            _context = context;
        }

        // Cand utilizatorul scrie in campul de nume, filtram lista
        partial void OnMedNameChanged(string value)
        {
            if (_suppressSearch) return;

            FilteredSuggestions.Clear();

            if (string.IsNullOrWhiteSpace(value) || value.Length < 1)
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

        // Utilizatorul selecteaza un medicament din dropdown
        [RelayCommand]
        void SelectSuggestion(MedicineSuggestion suggestion)
        {
            if (suggestion == null) return;

            // Suprima cautarea in timp ce setam numele programatic
            _suppressSearch = true;
            MedName  = suggestion.Name;
            Dosage   = suggestion.SuggestedDosage;
            _suppressSearch = false;

            FilteredSuggestions.Clear();
            ShowSuggestions = false;
        }

        // Inchide dropdown-ul (tap in afara)
        [RelayCommand]
        void DismissSuggestions()
        {
            ShowSuggestions = false;
        }

        partial void OnMedicineIdChanged(int value)
        {
            if (value > 0) LoadExistingMedicine(value);
        }

        private async void LoadExistingMedicine(int id)
        {
            var med = await _context.Medicines.FindAsync(id);
            if (med == null) return;

            _suppressSearch  = true;
            PageTitle        = "Edit Prescription";
            MedName          = med.Name;
            Dosage           = med.Dosage;
            InitialStock     = med.StockQuantity.ToString();
            PatientId        = med.PatientId;
            _suppressSearch  = false;

            var times = med.MealTimes ?? med.TimeOfDay ?? string.Empty;
            TakeMorning = times.Contains("Morning");
            TakeLunch   = times.Contains("Lunch");
            TakeDinner  = times.Contains("Dinner");
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

            // Stock default 30 daca nu s-a completat
            int stock = int.TryParse(InitialStock, out int s) && s > 0 ? s : 30;

            try
            {
                int currentUserId = Preferences.Default.Get("CurrentUserId", 0);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == currentUserId);
                var suggestion = MedicineDatabase.FindByName(MedName);

                Medicine medToSave;
                if (MedicineId > 0)
                {
                    medToSave = await _context.Medicines.FindAsync(MedicineId);
                    if (medToSave != null)
                    {
                        medToSave.Name          = MedName.Trim();
                        medToSave.Dosage        = string.IsNullOrWhiteSpace(Dosage) ? "As directed" : Dosage.Trim();
                        medToSave.MealTimes     = mealTimes;
                        medToSave.TimeOfDay     = mealTimes;
                        medToSave.Category      = suggestion?.Category ?? medToSave.Category;
                        medToSave.StockQuantity = stock;
                    }
                }
                else
                {
                    medToSave = new Medicine
                    {
                        Name                 = MedName.Trim(),
                        Dosage               = string.IsNullOrWhiteSpace(Dosage) ? "As directed" : Dosage.Trim(),
                        MealTimes            = mealTimes,
                        TimeOfDay            = mealTimes,
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
    }
}

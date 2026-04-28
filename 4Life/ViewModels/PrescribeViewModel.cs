using _4Life.Data;
using _4Life.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Numerics;
//using static Android.Net.Http.SslCertificate;

namespace _4Life.ViewModels
{
    // Ensure "patientId" (field) exists for "PatientId" (property) to be generated
    [QueryProperty(nameof(PatientId), "patientId")]
    [QueryProperty(nameof(MedicineId), "medicineId")]
    public partial class PrescribeViewModel : ObservableObject
    {

        [ObservableProperty] 
        private int medicineId;
        [ObservableProperty] 
        private string pageTitle = "New Prescription";

        private readonly AppDbContext _context;

        [ObservableProperty]
        private int patientId;

        [ObservableProperty]
        private string medName;

        [ObservableProperty]
        private string dosage;

        [ObservableProperty]
        private string selectedTime;

        [ObservableProperty]
        private string initialStock;

        public List<string> TimeSlots { get; } = new() { "Morning", "Lunch", "Evening" };

        public PrescribeViewModel(AppDbContext context)
        {
            _context = context;
        }
        
        partial void OnMedicineIdChanged(int value)
        {
            if (value > 0) LoadExistingMedicine(value);
        }

        private async void LoadExistingMedicine(int id)
        {
            var med = await _context.Medicines.FindAsync(id);
            if (med != null)
            {
                this.pageTitle = "Edit Prescription";
                MedName = med.Name;
                Dosage = med.Dosage;
                SelectedTime = med.TimeOfDay;
                InitialStock = med.StockQuantity.ToString();
                PatientId = med.PatientId;
            }
        }

        /*
        [RelayCommand]
        private async Task SavePrescription()
        {
            // Use the generated Property (Capital P)
            if (PatientId == 0)
            {
                await Shell.Current.DisplayAlert("Error", "Patient ID is 0. Navigation failed to pass data.", "OK");
                return;
            }

            try
            {
                int currentUserId = Preferences.Default.Get("CurrentUserId", 0);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == currentUserId);

                var newMed = new Medicine
                {
                    Name = MedName, // Use Generated Property
                    Dosage = Dosage,
                    TimeOfDay = SelectedTime,
                    PatientId = PatientId, // Use Generated Property
                    PrescribedByDoctorId = doctor?.Id,
                    TargetDate = DateTime.Today,
                    StockQuantity = int.TryParse(this.initialStock, out int s) ? s : 30,
                    IsTaken = false,
                    Category = "Prescription",
                    ExpiryDate = DateTime.Today.AddYears(1) // Ensure no null constraints on Expiry
                };

                _context.Medicines.Add(newMed);
                await _context.SaveChangesAsync();

                await Shell.Current.DisplayAlert("Success", "Medication added!", "OK");

                // This is what closes the page!
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                await Shell.Current.DisplayAlert("Database Error", message, "OK");
            }
        }

        */

        [RelayCommand]
        private async Task SavePrescription()
        {
            try
            {
                // 1. FETCH THE DOCTOR FIRST (This makes 'doctor' exist for the whole method)
                int currentUserId = Preferences.Default.Get("CurrentUserId", 0);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == currentUserId);

                Medicine medToSave;

                if (this.MedicineId > 0)
                {
                    // UPDATE EXISTING
                    medToSave = await _context.Medicines.FindAsync(this.MedicineId);
                    if (medToSave != null)
                    {
                        medToSave.Name = MedName;
                        medToSave.Dosage = Dosage;
                        medToSave.TimeOfDay = SelectedTime;
                        medToSave.StockQuantity = int.TryParse(InitialStock, out int s) ? s : medToSave.StockQuantity;
                        // No need to call .Add() here, Entity Framework tracks changes to medToSave automatically
                    }
                }
                else
                {
                    // CREATE NEW
                    medToSave = new Medicine
                    {
                        Name = MedName,
                        Dosage = Dosage,
                        TimeOfDay = SelectedTime,
                        PatientId = PatientId,
                        PrescribedByDoctorId = doctor?.Id, // NOW 'doctor' EXISTS HERE!
                        TargetDate = DateTime.Today,
                        StockQuantity = int.TryParse(InitialStock, out int s) ? s : 30,
                        IsTaken = false,
                        Category = "Prescription",
                        ExpiryDate = DateTime.Today.AddYears(1)
                    };
                    _context.Medicines.Add(medToSave);
                }

                // 2. SAVE EVERYTHING
                await _context.SaveChangesAsync();

                await Shell.Current.DisplayAlert("Success", "Prescription Saved!", "OK");
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                await Shell.Current.DisplayAlert("Database Error", message, "OK");
            }
        }

    }
}
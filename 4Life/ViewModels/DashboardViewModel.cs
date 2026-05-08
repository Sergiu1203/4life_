using _4Life.Data;
using _4Life.Models;
//using Android.Accounts;
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

        [ObservableProperty]
        private string userName;

        public ObservableCollection<Medicine> DailyMeds { get; set; } = new();

        public DashboardViewModel(AppDbContext context)
        {
            _context = context;
            //LoadData();
        }

        /*private async void LoadData()
        {
            var items = await _context.Medicines.ToListAsync();
 
            DailyMeds.Clear();
            foreach (var item in items)
                DailyMeds.Add(item);
        }*/

       public async Task LoadPatientData(int userId)
        {
            // 1. Check if the User -> Patient link exists
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (patient == null)
            {
                // If you see this in the Debug Output, your registration didn't link the Patient to the User properly
                System.Diagnostics.Debug.WriteLine($"ERROR: No patient found for User ID {userId}");
                return;
            }

            // 2. Fetch meds
            var meds = await _context.Medicines
                .Where(m => m.PatientId == patient.Id)
                .ToListAsync();

            // 3. Update the UI
            MainThread.BeginInvokeOnMainThread(() =>
            {
                DailyMeds.Clear();
                foreach (var med in meds)
                    DailyMeds.Add(med);
            });
        }
        /*
        [RelayCommand]
        public async Task ToggleMedicationTaken(Medicine med)
        {
            if (med.IsTaken)
            {
                // Decrement stock in the backend
                if (med.StockQuantity > 0)
                {
                    med.StockQuantity--;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    await Shell.Current.DisplayAlert("Low Stock", $"You are out of {med.Name}!", "OK");
                }
            }
        }
        */

        [RelayCommand]
        public async Task ToggleMedicationTaken(Medicine med)
        {
            if (med == null) return;

            var dbMed = await _context.Medicines.AsNoTracking().FirstOrDefaultAsync(m => m.Id == med.Id);

            if (dbMed != null && dbMed.IsTaken == med.IsTaken)
            {
                return;
            }

            int amountTaken = 1;
            var match = System.Text.RegularExpressions.Regex.Match(med.Dosage, @"\d+");
            if (match.Success) amountTaken = int.Parse(match.Value);

            if (med.IsTaken)
            {

                if (med.StockQuantity >= amountTaken)
                {
                    med.StockQuantity -= amountTaken;
                }
                else
                {
                    await Shell.Current.DisplayAlert("Out of Stock", "Not enough medicine left!", "OK");
                    med.IsTaken = false;
                    return;
                }
            }
            else
            {
                // BOX WAS UNCHECKED: Increase stock (returning the dose)
                med.StockQuantity += amountTaken;
            }

            _context.Medicines.Update(med);
            await _context.SaveChangesAsync();

        }




        [RelayCommand]
        private async Task AlertDoctor()
        {
            // 1. Get the message from the user
            string message = await Shell.Current.DisplayPromptAsync("Emergency",
                "Write a message for your doctor:", "Send", "Cancel");

            if (string.IsNullOrWhiteSpace(message)) return;

            try
            {
                // 2. Find the current patient
                int currentUserId = Preferences.Default.Get("CurrentUserId", 0);
                var patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserId == currentUserId);

                if (patient != null)
                {
                    patient.emergencyMessage = message;
                    patient.hasActiveAlert = true;

                    _context.Patients.Update(patient);
                    await _context.SaveChangesAsync();

                    await Shell.Current.DisplayAlert("Sent", "Your doctor has been notified.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Could not send alert.", "OK");
            }
        }

        [RelayCommand]
        private async Task SosContact()
        {
            string phoneNumber = "0722123456";
            if (PhoneDialer.Default.IsSupported)
                PhoneDialer.Default.Open(phoneNumber);
        }

        [RelayCommand]
        async Task GoToJournal()
        {
            await Shell.Current.GoToAsync("SymptomJournalPage");
        }

        [RelayCommand]
        async Task Logout()
        {
            // 1. Afișăm fereastra de confirmare
            bool answer = await Shell.Current.DisplayAlert("Logout",
                "Are you sure you want to logout?", "Yes", "No");

            // 2. Dacă utilizatorul a ales "Yes" (true), procedăm la logout
            if (answer)
            {
                // Ștergem ID-ul salvat
                Preferences.Default.Remove("CurrentUserId");

                // Navigăm înapoi la pagina de Login folosind ruta absolută
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }
    }
}
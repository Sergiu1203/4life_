using _4Life.Data;
using _4Life.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace _4Life.ViewModels
{
    [QueryProperty(nameof(PatientId), "patientId")]
    public partial class DailyCheckInViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty] private int patientId;
        [ObservableProperty] private int feelingScore = 5;
        [ObservableProperty] private bool hasHeadache;
        [ObservableProperty] private bool hasFatigue;
        [ObservableProperty] private bool hasNausea;
        [ObservableProperty] private bool hasDizziness;
        [ObservableProperty] private bool hasPain;
        [ObservableProperty] private bool hasBreathingDifficulty;
        [ObservableProperty] private bool hasAnxiety;
        [ObservableProperty] private bool hasFever;
        [ObservableProperty] private string notes = string.Empty;
        [ObservableProperty] private string scoreEmoji = "😐";
        [ObservableProperty] private string scoreLabel  = "Okay";

        public string TodayDate { get; } = DateTime.Now.ToString("dddd, dd MMMM");

        public DailyCheckInViewModel(AppDbContext context)
        {
            _context = context;
            UpdateScoreDisplay(5);
        }

        partial void OnFeelingScoreChanged(int value) => UpdateScoreDisplay(value);

        private void UpdateScoreDisplay(int score)
        {
            (ScoreEmoji, ScoreLabel) = score switch
            {
                1  => ("😫", "Very bad"),
                2  => ("😞", "Bad"),
                3  => ("😟", "Not great"),
                4  => ("😕", "Below average"),
                5  => ("😐", "Okay"),
                6  => ("🙂", "Decent"),
                7  => ("😊", "Good"),
                8  => ("😄", "Great"),
                9  => ("🤩", "Excellent"),
                10 => ("🌟", "Amazing!"),
                _  => ("😐", "Okay")
            };
        }

        private string BuildSymptomsList()
        {
            var s = new List<string>();
            if (HasHeadache)            s.Add("Headache");
            if (HasFatigue)             s.Add("Fatigue");
            if (HasNausea)              s.Add("Nausea");
            if (HasDizziness)           s.Add("Dizziness");
            if (HasPain)                s.Add("Pain");
            if (HasBreathingDifficulty) s.Add("Breathing difficulty");
            if (HasAnxiety)             s.Add("Anxiety");
            if (HasFever)               s.Add("Fever");
            return string.Join(", ", s);
        }

        [RelayCommand]
        async Task Submit()
        {
            var symptoms = BuildSymptomsList();

            // Construim intrarea in jurnal
            var sb = new StringBuilder();
            sb.AppendLine($"[Daily Check-in — {DateTime.Now:dd MMM yyyy HH:mm}]");
            sb.AppendLine($"Feeling: {FeelingScore}/10 ({ScoreLabel})");
            if (!string.IsNullOrEmpty(symptoms))
                sb.AppendLine($"Symptoms: {symptoms}");
            if (!string.IsNullOrWhiteSpace(Notes))
                sb.AppendLine($"Notes: {Notes.Trim()}");

            var patient = await _context.Patients
                .Include(p => p.AssignedDoctor)
                .FirstOrDefaultAsync(p => p.Id == PatientId);

            if (patient != null)
            {
                // Salvam in jurnal
                patient.JournalNotes = string.IsNullOrEmpty(patient.JournalNotes)
                    ? sb.ToString()
                    : patient.JournalNotes + "\n\n" + sb.ToString();

                // Daca scorul e sub 5, setam alerta pentru doctor
                if (FeelingScore < 5)
                {
                    string symptomPart = string.IsNullOrEmpty(symptoms)
                        ? "no specific symptoms reported"
                        : symptoms;

                    string alertMsg = $"[Check-in Alert] {patient.FullName} reported feeling {FeelingScore}/10 ({ScoreLabel}). " +
                                      $"Symptoms: {symptomPart}.";

                    // Folosim acelasi mecanism de alerta ca butonul SOS
                    patient.emergencyMessage = alertMsg;
                    patient.hasActiveAlert   = true;
                }

                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();
            }

            Preferences.Default.Set($"CheckIn_{PatientId}_{DateTime.Today:yyyy-MM-dd}", true);
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        async Task Skip()
        {
            Preferences.Default.Set($"CheckIn_{PatientId}_{DateTime.Today:yyyy-MM-dd}", true);
            await Shell.Current.GoToAsync("..");
        }
    }
}

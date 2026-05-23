using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _4Life.Models
{
    public partial class Medicine : ObservableObject
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Dosage    { get; set; }
        public string Category  { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string TimeOfDay { get; set; }
        public string? MealTimes { get; set; }
        public DateTime TargetDate { get; set; }

        [ForeignKey(nameof(Patient))]
        public int PatientId { get; set; }
        public Patient Patient { get; set; }

        public int? PrescribedByDoctorId { get; set; }
        public Doctor PrescribedByDoctor { get; set; }

        [ObservableProperty] private int stockQuantity;

        // IsTaken global — pastrat pentru compatibilitate cu codul existent (jurnal etc.)
        [ObservableProperty] private bool isTaken;

        // ─── Doze per moment al zilei ──────────────────────────────────
        public int MorningDose { get; set; }
        public int LunchDose   { get; set; }
        public int DinnerDose  { get; set; }

        // ─── IsTaken per moment — stocate in DB ────────────────────────
        // Permite decrementare independenta pentru fiecare moment al zilei
        public bool MorningTaken { get; set; }
        public bool LunchTaken   { get; set; }
        public bool DinnerTaken  { get; set; }

        // ─── Proprietati calculate ─────────────────────────────────────
        [NotMapped]
        public string DisplayTimes => string.IsNullOrEmpty(MealTimes)
            ? (TimeOfDay ?? string.Empty)
            : MealTimes.Replace(",", " · ");

        [NotMapped]
        public bool IsPatientAdded => PrescribedByDoctorId == null;

        [NotMapped]
        public string OriginLabel => IsPatientAdded ? "🧴 Added by patient" : string.Empty;

        // Returneaza doza pentru un moment al zilei
        public int GetDoseForTime(string mealTime) => mealTime switch
        {
            "Morning" => MorningDose > 0 ? MorningDose : 1,
            "Lunch"   => LunchDose   > 0 ? LunchDose   : 1,
            "Dinner"  => DinnerDose  > 0 ? DinnerDose  : 1,
            _         => 1
        };

        // Returneaza starea IsTaken pentru un moment al zilei din DB
        public bool GetIsTakenForTime(string mealTime) => mealTime switch
        {
            "Morning" => MorningTaken,
            "Lunch"   => LunchTaken,
            "Dinner"  => DinnerTaken,
            _         => IsTaken
        };

        // Seteaza starea IsTaken pentru un moment al zilei
        public void SetIsTakenForTime(string mealTime, bool value)
        {
            switch (mealTime)
            {
                case "Morning": MorningTaken = value; break;
                case "Lunch":   LunchTaken   = value; break;
                case "Dinner":  DinnerTaken  = value; break;
                default:        IsTaken      = value; break;
            }
            // Actualizeaza IsTaken global: true daca cel putin un moment e bifat
            IsTaken = MorningTaken || LunchTaken || DinnerTaken;
        }
    }
}

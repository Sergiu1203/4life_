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

        public string Dosage { get; set; }

        public string Category { get; set; }

        public DateTime ExpiryDate { get; set; }

        public string TimeOfDay { get; set; }

        public string? MealTimes { get; set; }

        public DateTime TargetDate { get; set; }

        [ForeignKey(nameof(Patient))]
        public int PatientId { get; set; }
        public Patient Patient { get; set; }

        public int? PrescribedByDoctorId { get; set; }
        public Doctor PrescribedByDoctor { get; set; }

        [ObservableProperty]
        private int stockQuantity;

        [ObservableProperty]
        private bool isTaken;

        // Afisare ore — "Morning · Lunch · Dinner"
        [NotMapped]
        public string DisplayTimes => string.IsNullOrEmpty(MealTimes)
            ? (TimeOfDay ?? string.Empty)
            : MealTimes.Replace(",", " · ");

        // True daca e adaugat de pacient (fara doctor)
        [NotMapped]
        public bool IsPatientAdded => PrescribedByDoctorId == null;

        // Badge pentru doctor: arata de unde vine medicamentul
        [NotMapped]
        public string OriginLabel => IsPatientAdded ? "🧴 Added by patient" : string.Empty;
    }
}

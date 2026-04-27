using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        //public int StockQuantity { get; set; }

        public DateTime ExpiryDate { get; set; }
        public string TimeOfDay { get; set; }
        //public bool IsTaken { get; set; }
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
    }
}

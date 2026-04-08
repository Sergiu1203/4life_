using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace _4Life.Models
{
    public class Medicine
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Dosage { get; set; } 

        public string Category { get; set; } 

        public int StockQuantity { get; set; }

        public DateTime ExpiryDate { get; set; } 

        
        public int PatientId { get; set; }
        public Patient Patient { get; set; }

        public int? PrescribedByDoctorId { get; set; }
        public Doctor PrescribedByDoctor { get; set; }
    }
}

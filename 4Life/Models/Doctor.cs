using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace _4Life.Models
{
    public class Doctor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; }

        public string? Specialization { get; set; }
        public string? MedicalId { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public List<Medicine> PrescribedMedicines { get; set; } = new();

        // Relatie many-to-many cu pacientii
        public List<PatientDoctor> PatientDoctors { get; set; } = new();

        // Mesaje chat
        public List<ChatMessage> ChatMessages { get; set; } = new();
    }
}

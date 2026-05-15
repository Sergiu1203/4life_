using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace _4Life.Models
{
    public class Patient
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; }

        public string? CNP { get; set; }
        public int? Age { get; set; }

        // Pastram DoctorId pentru compatibilitate (doctorul "principal"/primul)
        // dar acum relatia reala e many-to-many prin PatientDoctors
        public int? DoctorId { get; set; }
        public Doctor? AssignedDoctor { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public List<Medicine> PrescribedMedicines { get; set; } = new();

        public string? emergencyMessage { get; set; }
        public bool? hasActiveAlert { get; set; }
        public string? JournalNotes { get; set; }

        // Relatie many-to-many cu doctorii
        public List<PatientDoctor> PatientDoctors { get; set; } = new();

        // Mesaje chat
        public List<ChatMessage> ChatMessages { get; set; } = new();
    }
}

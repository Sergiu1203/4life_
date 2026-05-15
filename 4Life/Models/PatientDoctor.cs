using System.ComponentModel.DataAnnotations;

namespace _4Life.Models
{
    // Tabel de legătură pentru relația many-to-many Pacient <-> Doctor
    public class PatientDoctor
    {
        public int PatientId { get; set; }
        public Patient Patient { get; set; }

        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; }
    }
}

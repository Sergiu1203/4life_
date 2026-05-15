using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _4Life.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;

        // "Patient" sau "Doctor"
        public string SenderRole { get; set; }

        public int PatientId { get; set; }
        public Patient Patient { get; set; }

        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; }

        // -------------------------------------------------------
        // Proprietati calculate pentru UI (nu se salveaza in DB)
        // Setate de ChatViewModel dupa incarcare
        // -------------------------------------------------------

        [NotMapped]
        public bool IsSentByMe { get; set; }

        [NotMapped]
        public bool IsReceivedByMe => !IsSentByMe;

        // Numele afisat deasupra mesajelor primite (ex: "Dr. Ionescu")
        [NotMapped]
        public string SenderLabel { get; set; } = string.Empty;
    }
}

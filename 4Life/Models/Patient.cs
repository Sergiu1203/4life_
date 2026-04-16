using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        
        public int UserId { get; set; }
        public User User { get; set; }
    }
}

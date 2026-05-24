using _4Life.Models;
using Microsoft.EntityFrameworkCore;

namespace _4Life.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<PatientDoctor> PatientDoctors { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string dbPath = Path.Combine(FileSystem.AppDataDirectory, "4life_database_13.db3");
                optionsBuilder.UseSqlite($"Filename={dbPath}");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Patient -> User
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<Patient>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Doctor -> User
            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.User)
                .WithOne()
                .HasForeignKey<Doctor>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Medicine -> Patient
            modelBuilder.Entity<Medicine>()
                .HasOne(m => m.Patient)
                .WithMany(p => p.PrescribedMedicines)
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Medicine -> Doctor (prescriitor)
            modelBuilder.Entity<Medicine>()
                .HasOne(m => m.PrescribedByDoctor)
                .WithMany(d => d.PrescribedMedicines)
                .HasForeignKey(m => m.PrescribedByDoctorId)
                .OnDelete(DeleteBehavior.SetNull);

            // Many-to-many: Patient <-> Doctor prin PatientDoctor
            modelBuilder.Entity<PatientDoctor>()
                .HasKey(pd => new { pd.PatientId, pd.DoctorId });

            modelBuilder.Entity<PatientDoctor>()
                .HasOne(pd => pd.Patient)
                .WithMany(p => p.PatientDoctors)
                .HasForeignKey(pd => pd.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PatientDoctor>()
                .HasOne(pd => pd.Doctor)
                .WithMany(d => d.PatientDoctors)
                .HasForeignKey(pd => pd.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            // ChatMessage -> Patient
            modelBuilder.Entity<ChatMessage>()
                .HasOne(c => c.Patient)
                .WithMany(p => p.ChatMessages)
                .HasForeignKey(c => c.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // ChatMessage -> Doctor
            modelBuilder.Entity<ChatMessage>()
                .HasOne(c => c.Doctor)
                .WithMany(d => d.ChatMessages)
                .HasForeignKey(c => c.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

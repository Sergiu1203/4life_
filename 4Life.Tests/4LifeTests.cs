using Microsoft.EntityFrameworkCore;
using Xunit;
using System.ComponentModel.DataAnnotations;
using Windows.ApplicationModel.Chat;

namespace _4Life.Tests
{
    public class User
    {
        [Key] public int Id { get; set; }
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Role { get; set; } = "";
    }

    public class Doctor
    {
        [Key] public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string? Specialization { get; set; }
        public string? MedicalId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public List<Medicine> PrescribedMedicines { get; set; } = new();
        public List<PatientDoctor> PatientDoctors { get; set; } = new();
        public List<ChatMessage> ChatMessages { get; set; } = new();
    }

    public class Patient
    {
        [Key] public int Id { get; set; }
        public string FullName { get; set; } = "";
        public int? Age { get; set; }
        public int? DoctorId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public List<Medicine> PrescribedMedicines { get; set; } = new();
        public string? emergencyMessage { get; set; }
        public bool? hasActiveAlert { get; set; }
        public string? JournalNotes { get; set; }
        public List<PatientDoctor> PatientDoctors { get; set; } = new();
        public List<ChatMessage> ChatMessages { get; set; } = new();
    }

    public class Medicine
    {
        [Key] public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Dosage { get; set; } = "";
        public string? Category { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string? TimeOfDay { get; set; }
        public DateTime TargetDate { get; set; }
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
        public int? PrescribedByDoctorId { get; set; }
        public Doctor? PrescribedByDoctor { get; set; }
        public int StockQuantity { get; set; }
        public bool IsTaken { get; set; }
    }
    public class ChatMessage
    {
        [Key] public int Id { get; set; }
        public string Content { get; set; } = "";
        public DateTime SentAt { get; set; } = DateTime.Now;
        public string SenderRole { get; set; } = "";
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; } = null!;
        public bool IsSentByMe { get; set; }
        public string SenderLabel { get; set; } = "";
    }

    public class PatientDoctor
    {
        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; } = null!;
    }
    public static class MedicineDatabase
    {
        public static List<MedicineSuggestion> All { get; } = new()
        {
            new("Paracetamol",   "Analgesic",  "500mg"),
            new("Ibuprofen",     "Analgesic",  "400mg"),
            new("Aspirin",       "Analgesic",  "500mg"),
            new("Amoxicillin",   "Antibiotic", "500mg"),
            new("Azithromycin",  "Antibiotic", "500mg"),
            new("Vitamin C",     "Supplement", "500mg"),
            new("Vitamin D3",    "Supplement", "1000 IU"),
            new("Omeprazole",    "Gastrointestinal", "20mg"),
        };

        public static List<string> Categories =>
            All.Select(m => m.Category).Distinct().OrderBy(c => c).ToList();

        public static MedicineSuggestion? FindByName(string name) =>
            All.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public class MedicineSuggestion
    {
        public string Name { get; }
        public string Category { get; }
        public string SuggestedDosage { get; }
        public MedicineSuggestion(string name, string category, string dosage)
        { Name = name; Category = category; SuggestedDosage = dosage; }
    }

    // ============================================================
    // DB CONTEXT (InMemory — fara SQLite, fara FileSystem MAUI)
    // ============================================================
    public class TestDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Doctor> Doctors { get; set; }
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<PatientDoctor> PatientDoctors { get; set; }

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.User).WithOne()
                .HasForeignKey<Patient>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Doctor>()
                .HasOne(d => d.User).WithOne()
                .HasForeignKey<Doctor>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Medicine>()
                .HasOne(m => m.Patient).WithMany(p => p.PrescribedMedicines)
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Medicine>()
                .HasOne(m => m.PrescribedByDoctor).WithMany(d => d.PrescribedMedicines)
                .HasForeignKey(m => m.PrescribedByDoctorId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PatientDoctor>()
                .HasKey(pd => new { pd.PatientId, pd.DoctorId });

            modelBuilder.Entity<PatientDoctor>()
                .HasOne(pd => pd.Patient).WithMany(p => p.PatientDoctors)
                .HasForeignKey(pd => pd.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PatientDoctor>()
                .HasOne(pd => pd.Doctor).WithMany(d => d.PatientDoctors)
                .HasForeignKey(pd => pd.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(c => c.Patient).WithMany(p => p.ChatMessages)
                .HasForeignKey(c => c.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(c => c.Doctor).WithMany(d => d.ChatMessages)
                .HasForeignKey(c => c.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    // ============================================================
    // TESTE TC01 – TC16
    // ============================================================
    public class FourLifeTests
    {
        private TestDbContext CreateContext() =>
            new TestDbContext(new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
                .Options);

        // TC01 — Register patient with valid data
        [Fact]
        public async Task TC01_RegisterPatient_WithValidData_SavesPatientAndUser()
        {
            using var ctx = CreateContext();

            var doctorUser = new User { Email = "doc@test.com", Password = "Doc1!pass", Role = "Doctor" };
            ctx.Users.Add(doctorUser);
            await ctx.SaveChangesAsync();

            var doctor = new Doctor { FullName = "Dr. Popa", MedicalId = "MED001", UserId = doctorUser.Id };
            ctx.Doctors.Add(doctor);
            await ctx.SaveChangesAsync();

            var newUser = new User { Email = "patient@example.com", Password = "Secure1!", Role = "Patient" };
            var newPatient = new Patient { FullName = "Ion Popescu", Age = 30, DoctorId = doctor.Id, User = newUser };
            ctx.Patients.Add(newPatient);
            await ctx.SaveChangesAsync();

            var savedUser = await ctx.Users.FirstOrDefaultAsync(u => u.Email == "patient@example.com");
            var savedPatient = await ctx.Patients.FirstOrDefaultAsync(p => p.FullName == "Ion Popescu");

            Assert.NotNull(savedUser);
            Assert.NotNull(savedPatient);
            Assert.Equal("Patient", savedUser.Role);
            Assert.Equal(doctor.Id, savedPatient.DoctorId);
        }

        // TC02 — Register doctor with valid data
        [Fact]
        public async Task TC02_RegisterDoctor_WithValidData_SavesDoctorAndUser()
        {
            using var ctx = CreateContext();

            var newUser = new User { Email = "doctor@spital.ro", Password = "Doctor1!", Role = "Doctor" };
            var newDoctor = new Doctor { FullName = "Dr. Ionescu Maria", MedicalId = "MED-2024-001", Specialization = "Cardiology", User = newUser };
            ctx.Doctors.Add(newDoctor);
            await ctx.SaveChangesAsync();

            var savedUser = await ctx.Users.FirstOrDefaultAsync(u => u.Email == "doctor@spital.ro");
            var savedDoctor = await ctx.Doctors.FirstOrDefaultAsync(d => d.MedicalId == "MED-2024-001");

            Assert.NotNull(savedUser);
            Assert.NotNull(savedDoctor);
            Assert.Equal("Doctor", savedUser.Role);
            Assert.Equal("Cardiology", savedDoctor.Specialization);
        }

        // TC03 — Reject weak password
        [Fact]
        public void TC03_WeakPassword_FailsValidation()
        {
            var weakPasswords = new[] { "abc", "12345678", "password", "ALLCAPS1" };

            foreach (var pwd in weakPasswords)
            {
                bool isValid = pwd.Length >= 8 &&
                               pwd.Any(char.IsUpper) &&
                               pwd.Any(char.IsLower) &&
                               pwd.Any(char.IsDigit) &&
                               pwd.Any(ch => !char.IsLetterOrDigit(ch));

                Assert.False(isValid, $"Parola '{pwd}' ar trebui sa fie invalida.");
            }
        }

        // TC04 — Reject invalid email
        [Fact]
        public void TC04_InvalidEmail_FailsValidation()
        {
            var invalidEmails = new[] { "notanemail", "missing@", "@nodomain.com", "", "fara_at" };

            foreach (var email in invalidEmails)
            {
                bool isValid = !string.IsNullOrWhiteSpace(email) &&
                               email.Contains("@") &&
                               email.IndexOf("@") > 0 &&
                               email.IndexOf("@") < email.Length - 1;

                Assert.False(isValid, $"Email '{email}' ar trebui sa fie invalid.");
            }
        }

        // TC05 — Login with invalid credentials
        [Fact]
        public async Task TC05_Login_WithInvalidCredentials_ReturnsNull()
        {
            using var ctx = CreateContext();
            ctx.Users.Add(new User { Email = "real@test.com", Password = "Real1!pass", Role = "Patient" });
            await ctx.SaveChangesAsync();

            var found = await ctx.Users.FirstOrDefaultAsync(u => u.Email == "real@test.com" && u.Password == "WrongPass!");
            Assert.Null(found);
        }

        // TC06 — Login as patient
        [Fact]
        public async Task TC06_Login_AsPatient_LoadsPatientData()
        {
            using var ctx = CreateContext();

            var user = new User { Email = "patient@test.com", Password = "Patient1!", Role = "Patient" };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            ctx.Patients.Add(new Patient { FullName = "Ana Ionescu", Age = 25, UserId = user.Id });
            await ctx.SaveChangesAsync();

            var foundUser = await ctx.Users.FirstOrDefaultAsync(u => u.Email == "patient@test.com" && u.Password == "Patient1!");
            var foundPatient = await ctx.Patients.FirstOrDefaultAsync(p => p.UserId == foundUser!.Id);

            Assert.NotNull(foundUser);
            Assert.Equal("Patient", foundUser.Role);
            Assert.NotNull(foundPatient);
            Assert.Equal("Ana Ionescu", foundPatient.FullName);
        }

        // TC07 — Login as doctor
        [Fact]
        public async Task TC07_Login_AsDoctor_LoadsDoctorData()
        {
            using var ctx = CreateContext();

            var user = new User { Email = "dr.pop@clinic.ro", Password = "Doctor1!", Role = "Doctor" };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            ctx.Doctors.Add(new Doctor { FullName = "Dr. Pop Alexandru", MedicalId = "MED-999", UserId = user.Id });
            await ctx.SaveChangesAsync();

            var foundUser = await ctx.Users.FirstOrDefaultAsync(u => u.Email == "dr.pop@clinic.ro" && u.Password == "Doctor1!");
            var foundDoctor = await ctx.Doctors.FirstOrDefaultAsync(d => d.UserId == foundUser!.Id);

            Assert.NotNull(foundUser);
            Assert.Equal("Doctor", foundUser.Role);
            Assert.NotNull(foundDoctor);
            Assert.Equal("Dr. Pop Alexandru", foundDoctor.FullName);
        }

        // TC08 — Load patient medicines
        [Fact]
        public async Task TC08_LoadMedicines_ForPatient_ReturnsCorrectList()
        {
            using var ctx = CreateContext();

            var user = new User { Email = "p@t.com", Password = "Pass1!", Role = "Patient" };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var patient = new Patient { FullName = "Test Patient", UserId = user.Id };
            ctx.Patients.Add(patient);
            await ctx.SaveChangesAsync();

            ctx.Medicines.AddRange(
                new Medicine { Name = "Paracetamol", Dosage = "500mg", PatientId = patient.Id, StockQuantity = 20, ExpiryDate = DateTime.Now.AddYears(1), TargetDate = DateTime.Now },
                new Medicine { Name = "Ibuprofen", Dosage = "400mg", PatientId = patient.Id, StockQuantity = 10, ExpiryDate = DateTime.Now.AddYears(1), TargetDate = DateTime.Now }
            );
            await ctx.SaveChangesAsync();

            var meds = await ctx.Medicines.Where(m => m.PatientId == patient.Id).ToListAsync();

            Assert.Equal(2, meds.Count);
            Assert.Contains(meds, m => m.Name == "Paracetamol");
            Assert.Contains(meds, m => m.Name == "Ibuprofen");
        }

        // TC09 — Mark medicine as taken
        [Fact]
        public async Task TC09_MarkMedicineAsTaken_DecreasesStockCorrectly()
        {
            using var ctx = CreateContext();

            var user = new User { Email = "p@t.com", Password = "Pass1!", Role = "Patient" };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var patient = new Patient { FullName = "Test Patient", UserId = user.Id };
            ctx.Patients.Add(patient);
            await ctx.SaveChangesAsync();

            ctx.Medicines.Add(new Medicine { Name = "Aspirin", Dosage = "500mg", PatientId = patient.Id, StockQuantity = 15, ExpiryDate = DateTime.Now.AddYears(1), TargetDate = DateTime.Now });
            await ctx.SaveChangesAsync();

            var dbMed = await ctx.Medicines.FirstAsync();
            dbMed.StockQuantity -= 1;
            dbMed.IsTaken = true;
            await ctx.SaveChangesAsync();

            var updated = await ctx.Medicines.FindAsync(dbMed.Id);
            Assert.Equal(14, updated!.StockQuantity);
            Assert.True(updated.IsTaken);
        }

        // TC10 — Prevent invalid stock update
        [Fact]
        public async Task TC10_InsufficientStock_StockUnchanged()
        {
            using var ctx = CreateContext();

            var user = new User { Email = "p@t.com", Password = "Pass1!", Role = "Patient" };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var patient = new Patient { FullName = "Test Patient", UserId = user.Id };
            ctx.Patients.Add(patient);
            await ctx.SaveChangesAsync();

            ctx.Medicines.Add(new Medicine { Name = "Warfarin", Dosage = "5mg", PatientId = patient.Id, StockQuantity = 0, ExpiryDate = DateTime.Now.AddYears(1), TargetDate = DateTime.Now });
            await ctx.SaveChangesAsync();

            var dbMed = await ctx.Medicines.FirstAsync();
            bool warningTriggered = false;

            if (dbMed.StockQuantity < 1)
                warningTriggered = true;
            else
            {
                dbMed.StockQuantity -= 1;
                await ctx.SaveChangesAsync();
            }

            var unchanged = await ctx.Medicines.FindAsync(dbMed.Id);
            Assert.Equal(0, unchanged!.StockQuantity);
            Assert.True(warningTriggered);
        }

        // TC11 — Prescribe medicine
        [Fact]
        public async Task TC11_PrescribeMedicine_SavesWithCorrectReferences()
        {
            using var ctx = CreateContext();

            var doctorUser = new User { Email = "doc@t.com", Password = "Doc1!", Role = "Doctor" };
            var patientUser = new User { Email = "pat@t.com", Password = "Pat1!", Role = "Patient" };
            ctx.Users.AddRange(doctorUser, patientUser);
            await ctx.SaveChangesAsync();

            var doctor = new Doctor { FullName = "Dr. Test", MedicalId = "M01", UserId = doctorUser.Id };
            var patient = new Patient { FullName = "Patient Test", UserId = patientUser.Id };
            ctx.Doctors.Add(doctor);
            ctx.Patients.Add(patient);
            await ctx.SaveChangesAsync();

            ctx.Medicines.Add(new Medicine { Name = "Amoxicillin", Dosage = "500mg", PatientId = patient.Id, PrescribedByDoctorId = doctor.Id, StockQuantity = 30, ExpiryDate = DateTime.Now.AddYears(1), TargetDate = DateTime.Now });
            await ctx.SaveChangesAsync();

            var saved = await ctx.Medicines.Include(m => m.PrescribedByDoctor).FirstOrDefaultAsync(m => m.Name == "Amoxicillin");

            Assert.NotNull(saved);
            Assert.Equal(doctor.Id, saved.PrescribedByDoctorId);
            Assert.Equal(patient.Id, saved.PatientId);
            Assert.Equal("Dr. Test", saved.PrescribedByDoctor!.FullName);
        }

        // TC12 — Edit prescription
        [Fact]
        public async Task TC12_EditPrescription_UpdatesDosageAndStock()
        {
            using var ctx = CreateContext();

            var user = new User { Email = "p@t.com", Password = "Pass1!", Role = "Patient" };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var patient = new Patient { FullName = "Edit Patient", UserId = user.Id };
            ctx.Patients.Add(patient);
            await ctx.SaveChangesAsync();

            ctx.Medicines.Add(new Medicine { Name = "Ibuprofen", Dosage = "200mg", PatientId = patient.Id, StockQuantity = 10, ExpiryDate = DateTime.Now.AddYears(1), TargetDate = DateTime.Now });
            await ctx.SaveChangesAsync();

            var dbMed = await ctx.Medicines.FirstAsync();
            dbMed.Dosage = "400mg";
            dbMed.StockQuantity = 20;
            await ctx.SaveChangesAsync();

            var updated = await ctx.Medicines.FindAsync(dbMed.Id);
            Assert.Equal("400mg", updated!.Dosage);
            Assert.Equal(20, updated.StockQuantity);
        }

        // TC13 — Delete prescription
        [Fact]
        public async Task TC13_DeletePrescription_RemovesMedicineFromDatabase()
        {
            using var ctx = CreateContext();

            var user = new User { Email = "p@t.com", Password = "Pass1!", Role = "Patient" };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var patient = new Patient { FullName = "Delete Patient", UserId = user.Id };
            ctx.Patients.Add(patient);
            await ctx.SaveChangesAsync();

            var med = new Medicine { Name = "Prednisone", Dosage = "5mg", PatientId = patient.Id, StockQuantity = 5, ExpiryDate = DateTime.Now.AddYears(1), TargetDate = DateTime.Now };
            ctx.Medicines.Add(med);
            await ctx.SaveChangesAsync();

            int id = med.Id;
            ctx.Medicines.Remove(med);
            await ctx.SaveChangesAsync();

            Assert.Null(await ctx.Medicines.FindAsync(id));
        }

        // TC14 — Save symptom journal
        [Fact]
        public async Task TC14_SaveJournalNotes_PersistedCorrectly()
        {
            using var ctx = CreateContext();

            var user = new User { Email = "p@t.com", Password = "Pass1!", Role = "Patient" };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var patient = new Patient { FullName = "Journal Patient", UserId = user.Id };
            ctx.Patients.Add(patient);
            await ctx.SaveChangesAsync();

            var dbPatient = await ctx.Patients.FindAsync(patient.Id);
            dbPatient!.JournalNotes = "Durere de cap si oboseala.";
            await ctx.SaveChangesAsync();

            var saved = await ctx.Patients.FindAsync(patient.Id);
            Assert.NotNull(saved!.JournalNotes);
            Assert.Contains("Durere de cap", saved.JournalNotes);
        }

        // TC15 — Send emergency alert
        [Fact]
        public async Task TC15_SendEmergencyAlert_AlertIsAccessibleToDoctor()
        {
            using var ctx = CreateContext();

            var user = new User { Email = "pat@t.com", Password = "Pat1!", Role = "Patient" };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var patient = new Patient { FullName = "Alert Patient", UserId = user.Id };
            ctx.Patients.Add(patient);
            await ctx.SaveChangesAsync();

            var dbPatient = await ctx.Patients.FindAsync(patient.Id);
            dbPatient!.emergencyMessage = "Am dureri puternice in piept!";
            dbPatient.hasActiveAlert = true;
            await ctx.SaveChangesAsync();

            var result = await ctx.Patients.FirstOrDefaultAsync(p => p.Id == patient.Id && p.hasActiveAlert == true);
            Assert.NotNull(result);
            Assert.True(result.hasActiveAlert);
            Assert.Equal("Am dureri puternice in piept!", result.emergencyMessage);
        }

        // TC16 — Delete patient account
        [Fact]
        public async Task TC16_DeletePatient_RemovesPatientAndUserData()
        {
            using var ctx = CreateContext();

            var user = new User { Email = "todelete@test.com", Password = "Del1!pass", Role = "Patient" };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var patient = new Patient { FullName = "Patient DeEsters", UserId = user.Id };
            ctx.Patients.Add(patient);
            await ctx.SaveChangesAsync();

            int patientId = patient.Id;
            int userId = user.Id;

            ctx.Patients.Remove(patient);
            ctx.Users.Remove(user);
            await ctx.SaveChangesAsync();

            Assert.Null(await ctx.Users.FindAsync(userId));
            Assert.Null(await ctx.Patients.FindAsync(patientId));
        }
    
    // ================================================================
        // PASSWORD RESET — Verify email exists
        // ================================================================
        [Fact]
        public async Task ForgotPassword_ExistingEmail_ReturnsUser()
        {
            using var ctx = CreateContext();
            ctx.Users.Add(new User { Email = "exist@test.com", Password = "Pass1!", Role = "Patient" });
            await ctx.SaveChangesAsync();

            var user = await ctx.Users.FirstOrDefaultAsync(u => u.Email == "exist@test.com");
            Assert.NotNull(user);
        }

        [Fact]
        public async Task ForgotPassword_NonExistingEmail_ReturnsNull()
        {
            using var ctx = CreateContext();

            var user = await ctx.Users.FirstOrDefaultAsync(u => u.Email == "ghost@test.com");
            Assert.Null(user);
        }

        // ================================================================
        // PASSWORD RESET — Reset password saves new value
        // ================================================================
        [Fact]
        public async Task ForgotPassword_ResetPassword_UpdatesCorrectly()
        {
            using var ctx = CreateContext();
            ctx.Users.Add(new User { Email = "reset@test.com", Password = "OldPass1!", Role = "Patient" });
            await ctx.SaveChangesAsync();

            var user = await ctx.Users.FirstOrDefaultAsync(u => u.Email == "reset@test.com");
            user!.Password = "NewPass1!";
            ctx.Users.Update(user);
            await ctx.SaveChangesAsync();

            var updated = await ctx.Users.FirstOrDefaultAsync(u => u.Email == "reset@test.com");
            Assert.Equal("NewPass1!", updated!.Password);
        }

        [Fact]
        public void ForgotPassword_MismatchedPasswords_FailsValidation()
        {
            string newPassword = "NewPass1!";
            string confirmPassword = "Different1!";

            bool match = newPassword == confirmPassword;
            Assert.False(match);
        }

        // ================================================================
        // DAILY CHECK-IN — Score display
        // ================================================================
        [Theory]
        [InlineData(1, "Very bad")]
        [InlineData(5, "Okay")]
        [InlineData(7, "Good")]
        [InlineData(10, "Amazing!")]
        public void DailyCheckIn_ScoreLabel_IsCorrect(int score, string expectedLabel)
        {
            string label = score switch
            {
                1 => "Very bad",
                2 => "Bad",
                3 => "Not great",
                4 => "Below average",
                5 => "Okay",
                6 => "Decent",
                7 => "Good",
                8 => "Great",
                9 => "Excellent",
                10 => "Amazing!",
                _ => "Okay"
            };
            Assert.Equal(expectedLabel, label);
        }

        // ================================================================
        // DAILY CHECK-IN — Low score triggers alert
        // ================================================================
        [Fact]
        public async Task DailyCheckIn_LowScore_SetsEmergencyAlert()
        {
            using var ctx = CreateContext();
            var user = new User { Email = "p@t.com", Password = "Pass1!", Role = "Patient" };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var patient = new Patient { FullName = "Check-in Patient", UserId = user.Id };
            ctx.Patients.Add(patient);
            await ctx.SaveChangesAsync();

            // Simuleaza submit cu scor < 5
            int feelingScore = 3;
            string symptoms = "Headache, Fatigue";

            var dbPatient = await ctx.Patients.FindAsync(patient.Id);
            if (feelingScore < 5)
            {
                dbPatient!.emergencyMessage = $"[Check-in Alert] feeling {feelingScore}/10. Symptoms: {symptoms}.";
                dbPatient.hasActiveAlert = true;
            }
            await ctx.SaveChangesAsync();

            var result = await ctx.Patients.FindAsync(patient.Id);
            Assert.True(result!.hasActiveAlert);
            Assert.Contains("Check-in Alert", result.emergencyMessage);
        }

        [Fact]
        public async Task DailyCheckIn_HighScore_DoesNotTriggerAlert()
        {
            using var ctx = CreateContext();
            var user = new User { Email = "p@t.com", Password = "Pass1!", Role = "Patient" };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var patient = new Patient { FullName = "Happy Patient", UserId = user.Id };
            ctx.Patients.Add(patient);
            await ctx.SaveChangesAsync();

            int feelingScore = 8;
            var dbPatient = await ctx.Patients.FindAsync(patient.Id);
            if (feelingScore < 5)
            {
                dbPatient!.hasActiveAlert = true;
            }
            await ctx.SaveChangesAsync();

            var result = await ctx.Patients.FindAsync(patient.Id);
            Assert.True(result!.hasActiveAlert != true);
        }

        // ================================================================
        // DAILY CHECK-IN — Journal entry is saved
        // ================================================================
        [Fact]
        public async Task DailyCheckIn_Submit_SavesJournalEntry()
        {
            using var ctx = CreateContext();
            var user = new User { Email = "p@t.com", Password = "Pass1!", Role = "Patient" };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var patient = new Patient { FullName = "Journal Patient", UserId = user.Id };
            ctx.Patients.Add(patient);
            await ctx.SaveChangesAsync();

            string entry = $"[Daily Check-in — {DateTime.Now:dd MMM yyyy}]\nFeeling: 7/10 (Good)";

            var dbPatient = await ctx.Patients.FindAsync(patient.Id);
            dbPatient!.JournalNotes = entry;
            await ctx.SaveChangesAsync();

            var saved = await ctx.Patients.FindAsync(patient.Id);
            Assert.NotNull(saved!.JournalNotes);
            Assert.StartsWith("[Daily Check-in", saved.JournalNotes);
        }

        // ================================================================
        // CHAT — Send message saves to database
        // ================================================================
        [Fact]
        public async Task Chat_SendMessage_SavedToDatabase()
        {
            using var ctx = CreateContext();

            var doctorUser = new User { Email = "doc@t.com", Password = "Doc1!", Role = "Doctor" };
            var patientUser = new User { Email = "pat@t.com", Password = "Pat1!", Role = "Patient" };
            ctx.Users.AddRange(doctorUser, patientUser);
            await ctx.SaveChangesAsync();

            var doctor = new Doctor { FullName = "Dr. Chat", MedicalId = "M01", UserId = doctorUser.Id };
            var patient = new Patient { FullName = "Chat Patient", UserId = patientUser.Id };
            ctx.Doctors.Add(doctor);
            ctx.Patients.Add(patient);
            await ctx.SaveChangesAsync();

            var message = new ChatMessage
            {
                Content = "Hello doctor!",
                SenderRole = "Patient",
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                SentAt = DateTime.Now
            };
            ctx.ChatMessages.Add(message);
            await ctx.SaveChangesAsync();

            var saved = await ctx.ChatMessages.FirstOrDefaultAsync(m => m.PatientId == patient.Id);
            Assert.NotNull(saved);
            Assert.Equal("Hello doctor!", saved.Content);
            Assert.Equal("Patient", saved.SenderRole);
        }

        // ================================================================
        // CHAT — Load messages returns correct conversation
        // ================================================================
        [Fact]
        public async Task Chat_LoadMessages_ReturnsCorrectConversation()
        {
            using var ctx = CreateContext();

            var doctorUser = new User { Email = "doc@t.com", Password = "Doc1!", Role = "Doctor" };
            var patientUser = new User { Email = "pat@t.com", Password = "Pat1!", Role = "Patient" };
            ctx.Users.AddRange(doctorUser, patientUser);
            await ctx.SaveChangesAsync();

            var doctor = new Doctor { FullName = "Dr. Load", MedicalId = "M02", UserId = doctorUser.Id };
            var patient = new Patient { FullName = "Load Patient", UserId = patientUser.Id };
            ctx.Doctors.Add(doctor);
            ctx.Patients.Add(patient);
            await ctx.SaveChangesAsync();

            ctx.ChatMessages.AddRange(
                new ChatMessage { Content = "Hello!", SenderRole = "Patient", PatientId = patient.Id, DoctorId = doctor.Id, SentAt = DateTime.Now.AddMinutes(-2) },
                new ChatMessage { Content = "Hi there!", SenderRole = "Doctor", PatientId = patient.Id, DoctorId = doctor.Id, SentAt = DateTime.Now.AddMinutes(-1) }
            );
            await ctx.SaveChangesAsync();

            var messages = await ctx.ChatMessages
                .Where(m => m.PatientId == patient.Id && m.DoctorId == doctor.Id)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            Assert.Equal(2, messages.Count);
            Assert.Equal("Patient", messages[0].SenderRole);
            Assert.Equal("Doctor", messages[1].SenderRole);
        }

        // ================================================================
        // SELECT DOCTORS — Patient assigned to multiple doctors
        // ================================================================
        [Fact]
        public async Task SelectDoctors_AssignMultipleDoctors_SavesAllLinks()
        {
            using var ctx = CreateContext();

            var patientUser = new User { Email = "pat@t.com", Password = "Pat1!", Role = "Patient" };
            var doc1User = new User { Email = "d1@t.com", Password = "Doc1!", Role = "Doctor" };
            var doc2User = new User { Email = "d2@t.com", Password = "Doc2!", Role = "Doctor" };
            ctx.Users.AddRange(patientUser, doc1User, doc2User);
            await ctx.SaveChangesAsync();

            var patient = new Patient { FullName = "Multi Patient", UserId = patientUser.Id };
            var doctor1 = new Doctor { FullName = "Dr. One", MedicalId = "M01", UserId = doc1User.Id };
            var doctor2 = new Doctor { FullName = "Dr. Two", MedicalId = "M02", UserId = doc2User.Id };
            ctx.Patients.Add(patient);
            ctx.Doctors.AddRange(doctor1, doctor2);
            await ctx.SaveChangesAsync();

            ctx.PatientDoctors.AddRange(
                new PatientDoctor { PatientId = patient.Id, DoctorId = doctor1.Id },
                new PatientDoctor { PatientId = patient.Id, DoctorId = doctor2.Id }
            );
            await ctx.SaveChangesAsync();

            var links = await ctx.PatientDoctors
                .Where(pd => pd.PatientId == patient.Id)
                .ToListAsync();

            Assert.Equal(2, links.Count);
        }

        [Fact]
        public async Task SelectDoctors_UpdateSelection_RemovesOldAndAddsNew()
        {
            using var ctx = CreateContext();

            var patientUser = new User { Email = "pat@t.com", Password = "Pat1!", Role = "Patient" };
            var doc1User = new User { Email = "d1@t.com", Password = "Doc1!", Role = "Doctor" };
            var doc2User = new User { Email = "d2@t.com", Password = "Doc2!", Role = "Doctor" };
            ctx.Users.AddRange(patientUser, doc1User, doc2User);
            await ctx.SaveChangesAsync();

            var patient = new Patient { FullName = "Switch Patient", UserId = patientUser.Id };
            var doctor1 = new Doctor { FullName = "Dr. Old", MedicalId = "M01", UserId = doc1User.Id };
            var doctor2 = new Doctor { FullName = "Dr. New", MedicalId = "M02", UserId = doc2User.Id };
            ctx.Patients.Add(patient);
            ctx.Doctors.AddRange(doctor1, doctor2);
            await ctx.SaveChangesAsync();

            // Initial — doctor1
            ctx.PatientDoctors.Add(new PatientDoctor { PatientId = patient.Id, DoctorId = doctor1.Id });
            await ctx.SaveChangesAsync();

            // Update — sterge toate si adauga doctor2
            var existing = await ctx.PatientDoctors.Where(pd => pd.PatientId == patient.Id).ToListAsync();
            ctx.PatientDoctors.RemoveRange(existing);
            ctx.PatientDoctors.Add(new PatientDoctor { PatientId = patient.Id, DoctorId = doctor2.Id });
            await ctx.SaveChangesAsync();

            var links = await ctx.PatientDoctors.Where(pd => pd.PatientId == patient.Id).ToListAsync();
            Assert.Single(links);
            Assert.Equal(doctor2.Id, links[0].DoctorId);
        }

        // ================================================================
        // ADD OWN MEDICINE — Patient adds OTC medicine
        // ================================================================
        [Fact]
        public async Task AddOwnMedicine_SavesWithNullDoctor()
        {
            using var ctx = CreateContext();
            var user = new User { Email = "p@t.com", Password = "Pass1!", Role = "Patient" };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var patient = new Patient { FullName = "OTC Patient", UserId = user.Id };
            ctx.Patients.Add(patient);
            await ctx.SaveChangesAsync();

            var med = new Medicine
            {
                Name = "Vitamin C",
                Dosage = "500mg",
                PatientId = patient.Id,
                PrescribedByDoctorId = null,  // adaugat de pacient, nu de doctor
                StockQuantity = 30,
                ExpiryDate = DateTime.Now.AddYears(1),
                TargetDate = DateTime.Now
            };
            ctx.Medicines.Add(med);
            await ctx.SaveChangesAsync();

            var saved = await ctx.Medicines.FirstOrDefaultAsync(m => m.Name == "Vitamin C");
            Assert.NotNull(saved);
            Assert.Null(saved.PrescribedByDoctorId);
        }

        [Fact]
        public void AddOwnMedicine_BuildMealTimes_CorrectString()
        {
            bool morning = true;
            bool lunch = false;
            bool dinner = true;

            var parts = new List<string>();
            if (morning) parts.Add("Morning");
            if (lunch) parts.Add("Lunch");
            if (dinner) parts.Add("Dinner");
            string result = string.Join(",", parts);

            Assert.Equal("Morning,Dinner", result);
        }

        [Fact]
        public void AddOwnMedicine_NoMealTimeSelected_ValidationFails()
        {
            bool morning = false;
            bool lunch = false;
            bool dinner = false;

            var parts = new List<string>();
            if (morning) parts.Add("Morning");
            if (lunch) parts.Add("Lunch");
            if (dinner) parts.Add("Dinner");
            string mealTimes = string.Join(",", parts);

            Assert.True(string.IsNullOrEmpty(mealTimes));
        }

        // ================================================================
        // ADMIN — Delete doctor reassigns patients
        // ================================================================
        [Fact]
        public async Task Admin_DeleteDoctor_PatientsReassignedToReplacement()
        {
            using var ctx = CreateContext();

            var d1User = new User { Email = "d1@t.com", Password = "Doc1!", Role = "Doctor" };
            var d2User = new User { Email = "d2@t.com", Password = "Doc2!", Role = "Doctor" };
            var pUser = new User { Email = "p@t.com", Password = "Pat1!", Role = "Patient" };
            ctx.Users.AddRange(d1User, d2User, pUser);
            await ctx.SaveChangesAsync();

            var doctor1 = new Doctor { FullName = "Dr. ToDelete", MedicalId = "M01", Specialization = "Cardiology", UserId = d1User.Id };
            var replacement = new Doctor { FullName = "Dr. Replacement", MedicalId = "M02", Specialization = "Cardiology", UserId = d2User.Id };
            var patient = new Patient { FullName = "Reassign Patient", UserId = pUser.Id };
            ctx.Doctors.AddRange(doctor1, replacement);
            ctx.Patients.Add(patient);
            await ctx.SaveChangesAsync();

            patient.DoctorId = doctor1.Id;
            ctx.Patients.Update(patient);
            await ctx.SaveChangesAsync();

            // Simuleaza delete doctor cu reatribuire
            var patients = await ctx.Patients.Where(p => p.DoctorId == doctor1.Id).ToListAsync();
            var rep = await ctx.Doctors.FirstOrDefaultAsync(d => d.Id != doctor1.Id && d.Specialization == doctor1.Specialization);

            foreach (var p in patients)
                p.DoctorId = rep?.Id;
            ctx.Patients.UpdateRange(patients);

            var user = await ctx.Users.FindAsync(doctor1.UserId);
            if (user != null) ctx.Users.Remove(user);
            await ctx.SaveChangesAsync();

            var updatedPatient = await ctx.Patients.FindAsync(patient.Id);
            Assert.Equal(replacement.Id, updatedPatient!.DoctorId);
        }

        [Fact]
        public async Task Admin_DeleteDoctor_NoReplacement_PatientsUnassigned()
        {
            using var ctx = CreateContext();

            var dUser = new User { Email = "d@t.com", Password = "Doc1!", Role = "Doctor" };
            var pUser = new User { Email = "p@t.com", Password = "Pat1!", Role = "Patient" };
            ctx.Users.AddRange(dUser, pUser);
            await ctx.SaveChangesAsync();

            var doctor = new Doctor { FullName = "Dr. Solo", MedicalId = "M01", UserId = dUser.Id };
            var patient = new Patient { FullName = "Solo Patient", UserId = pUser.Id };
            ctx.Doctors.Add(doctor);
            ctx.Patients.Add(patient);
            await ctx.SaveChangesAsync();

            patient.DoctorId = doctor.Id;
            ctx.Patients.Update(patient);
            await ctx.SaveChangesAsync();

            // Nu exista alt doctor — pacientul ramane fara doctor
            var patients = await ctx.Patients.Where(p => p.DoctorId == doctor.Id).ToListAsync();
            var rep = await ctx.Doctors.FirstOrDefaultAsync(d => d.Id != doctor.Id);

            foreach (var p in patients)
                p.DoctorId = rep?.Id;  // null
            ctx.Patients.UpdateRange(patients);

            var user = await ctx.Users.FindAsync(doctor.UserId);
            if (user != null) ctx.Users.Remove(user);
            await ctx.SaveChangesAsync();

            var updatedPatient = await ctx.Patients.FindAsync(patient.Id);
            Assert.Null(updatedPatient!.DoctorId);
        }

        // ================================================================
        // MEDICINE DATABASE — Search suggestions
        // ================================================================
        [Fact]
        public void MedicineDatabase_SearchByPartialName_ReturnsMatches()
        {
            // Simuleaza logica de search din AddOwnMedicineViewModel / PrescribeViewModel
            string query = "par";
            var matches = MedicineDatabase.All
                .Where(m => m.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(8)
                .ToList();

            Assert.NotEmpty(matches);
            Assert.Contains(matches, m => m.Name == "Paracetamol");
        }

        [Fact]
        public void MedicineDatabase_AllCategories_NotEmpty()
        {
            var categories = MedicineDatabase.Categories;
            Assert.NotEmpty(categories);
            Assert.Contains("Analgesic", categories);
            Assert.Contains("Antibiotic", categories);
        }

        [Fact]
        public void MedicineDatabase_StockDefault_IsThirty()
        {
            string initialStock = "abc";
            int stock = int.TryParse(initialStock, out int s) && s > 0 ? s : 30;
            Assert.Equal(30, stock);
        }

        // ================================================================
        // SYMPTOM JOURNAL — Manual notes vs check-in separation
        // ================================================================
        [Fact]
        public void SymptomJournal_SeparatesManualNotesFromCheckIns()
        {
            string journalNotes = "Durere de cap.\n\n[Daily Check-in — 19 May 2026]\nFeeling: 6/10";

            var allEntries = journalNotes.Split("\n\n", StringSplitOptions.RemoveEmptyEntries).ToList();

            var manual = allEntries.Where(e => !e.TrimStart().StartsWith("[Daily Check-in")).ToList();
            var checkIns = allEntries.Where(e => e.TrimStart().StartsWith("[Daily Check-in")).ToList();

            Assert.Single(manual);
            Assert.Single(checkIns);
            Assert.Equal("Durere de cap.", manual[0]);
        }

        [Fact]
        public async Task SymptomJournal_SaveManualNotes_PreservesCheckIns()
        {
            using var ctx = CreateContext();
            var user = new User { Email = "p@t.com", Password = "Pass1!", Role = "Patient" };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var patient = new Patient
            {
                FullName = "Journal Patient",
                UserId = user.Id,
                JournalNotes = "[Daily Check-in — 19 May]\nFeeling: 7/10"
            };
            ctx.Patients.Add(patient);
            await ctx.SaveChangesAsync();

            var dbPatient = await ctx.Patients.FindAsync(patient.Id);
            var existingCheckIns = dbPatient!.JournalNotes!
                .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
                .Where(e => e.TrimStart().StartsWith("[Daily Check-in"))
                .ToList();

            string newManualNote = "Ma simt mai bine azi.";
            var parts = new List<string> { newManualNote };
            parts.AddRange(existingCheckIns);
            dbPatient.JournalNotes = string.Join("\n\n", parts);
            await ctx.SaveChangesAsync();

            var saved = await ctx.Patients.FindAsync(patient.Id);
            Assert.Contains("Ma simt mai bine azi.", saved!.JournalNotes);
            Assert.Contains("[Daily Check-in", saved.JournalNotes);
        }
    } 
}
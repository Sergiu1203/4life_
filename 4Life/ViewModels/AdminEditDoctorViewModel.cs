using _4Life.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace _4Life.ViewModels
{
    [QueryProperty(nameof(DoctorId), "doctorId")]
    public partial class AdminEditDoctorViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty] private int    doctorId;
        [ObservableProperty] private string fullName       = string.Empty;
        [ObservableProperty] private string specialization = string.Empty;
        [ObservableProperty] private string medicalId      = string.Empty;
        [ObservableProperty] private string email          = string.Empty;

        public AdminEditDoctorViewModel(AppDbContext context) => _context = context;

        partial void OnDoctorIdChanged(int value) => LoadData(value);

        private async void LoadData(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return;

            FullName       = doctor.FullName       ?? string.Empty;
            Specialization = doctor.Specialization ?? string.Empty;
            MedicalId      = doctor.MedicalId      ?? string.Empty;

            var user = await _context.Users.FindAsync(doctor.UserId);
            Email = user?.Email ?? string.Empty;
        }

        [RelayCommand]
        async Task Save()
        {
            if (string.IsNullOrWhiteSpace(FullName))
            {
                await Shell.Current.DisplayAlert("Error", "Full name is required.", "OK");
                return;
            }

            var doctor = await _context.Doctors.FindAsync(DoctorId);
            var user   = await _context.Users.FindAsync(doctor?.UserId);

            if (doctor != null)
            {
                doctor.FullName       = FullName.Trim();
                doctor.Specialization = Specialization.Trim();
                doctor.MedicalId      = MedicalId.Trim();
                _context.Doctors.Update(doctor);
            }

            if (user != null && !string.IsNullOrWhiteSpace(Email))
            {
                user.Email = Email.Trim();
                _context.Users.Update(user);
            }

            await _context.SaveChangesAsync();
            await Shell.Current.DisplayAlert("Done", "Doctor updated successfully.", "OK");
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        async Task Cancel() => await Shell.Current.GoToAsync("..");
    }
}

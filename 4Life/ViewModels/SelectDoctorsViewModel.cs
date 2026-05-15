using _4Life.Data;
using _4Life.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace _4Life.ViewModels
{
    // ViewModel folosit de SelectDoctorsPage
    // Permite pacientului sa aleaga mai multi doctori (many-to-many)
    [QueryProperty(nameof(PatientId), "patientId")]
    public partial class SelectDoctorsViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty] private int patientId;

        // Wrapper cu flag "IsSelected" pentru UI
        public ObservableCollection<DoctorSelectionItem> Doctors { get; } = new();

        public SelectDoctorsViewModel(AppDbContext context)
        {
            _context = context;
        }

        public async Task LoadDoctors()
        {
            var allDoctors = await _context.Doctors.AsNoTracking().ToListAsync();

            var currentLinks = await _context.PatientDoctors
                .Where(pd => pd.PatientId == PatientId)
                .Select(pd => pd.DoctorId)
                .ToListAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Doctors.Clear();
                foreach (var d in allDoctors)
                {
                    Doctors.Add(new DoctorSelectionItem
                    {
                        Doctor = d,
                        IsSelected = currentLinks.Contains(d.Id)
                    });
                }
            });
        }

        [RelayCommand]
        public async Task SaveSelection()
        {
            // Sterge toate legaturile existente ale pacientului
            var existingLinks = await _context.PatientDoctors
                .Where(pd => pd.PatientId == PatientId)
                .ToListAsync();
            _context.PatientDoctors.RemoveRange(existingLinks);

            // Adauga cele selectate
            var selected = Doctors.Where(d => d.IsSelected).ToList();
            foreach (var item in selected)
            {
                _context.PatientDoctors.Add(new PatientDoctor
                {
                    PatientId = PatientId,
                    DoctorId = item.Doctor.Id
                });
            }

            // Actualizeaza si DoctorId (doctorul principal = primul selectat)
            var patient = await _context.Patients.FindAsync(PatientId);
            if (patient != null)
            {
                patient.DoctorId = selected.FirstOrDefault()?.Doctor.Id;
                _context.Patients.Update(patient);
            }

            await _context.SaveChangesAsync();
            await Shell.Current.DisplayAlert("Saved", "Your doctors have been updated.", "OK");
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        async Task GoBack() => await Shell.Current.GoToAsync("..");
    }

    // Clasa wrapper pentru UI cu checkbox
    public partial class DoctorSelectionItem : ObservableObject
    {
        public Doctor Doctor { get; set; }
        [ObservableProperty] private bool isSelected;
    }
}

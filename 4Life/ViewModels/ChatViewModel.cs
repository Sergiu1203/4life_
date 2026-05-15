using _4Life.Data;
using _4Life.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace _4Life.ViewModels
{
    [QueryProperty(nameof(PatientId),  "patientId")]
    [QueryProperty(nameof(DoctorId),   "doctorId")]
    [QueryProperty(nameof(SenderRole), "senderRole")]
    public partial class ChatViewModel : ObservableObject
    {
        private readonly AppDbContext _context;

        [ObservableProperty] private int patientId;
        [ObservableProperty] private int doctorId;
        [ObservableProperty] private string senderRole; // "Patient" sau "Doctor"
        [ObservableProperty] private string newMessage;
        [ObservableProperty] private string chatTitle = "Chat";

        // Numele celuilalt participant (afisat deasupra mesajelor primite)
        private string _otherName = string.Empty;

        public ObservableCollection<ChatMessage> Messages { get; } = new();

        public ChatViewModel(AppDbContext context)
        {
            _context = context;
        }

        public async Task LoadMessages()
        {
            var messages = await _context.ChatMessages
                .Where(m => m.PatientId == PatientId && m.DoctorId == DoctorId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            var patient = await _context.Patients.FindAsync(PatientId);
            var doctor  = await _context.Doctors.FindAsync(DoctorId);

            string patientName = patient?.FullName ?? "Patient";
            string doctorName  = doctor != null ? $"Dr. {doctor.FullName}" : "Doctor";

            // Titlul conversatiei
            ChatTitle = $"{patientName} ↔ {doctorName}";

            // Numele expeditorului pentru mesajele primite
            // Daca eu sunt pacientul, cel care imi scrie e doctorul (si invers)
            _otherName = SenderRole == "Patient" ? doctorName : patientName;

            // Marcam fiecare mesaj: IsSentByMe = trimis de rolul curent
            foreach (var msg in messages)
            {
                msg.IsSentByMe   = msg.SenderRole == SenderRole;
                msg.SenderLabel  = msg.IsSentByMe ? string.Empty : _otherName;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Messages.Clear();
                foreach (var msg in messages)
                    Messages.Add(msg);
            });
        }

        [RelayCommand]
        public async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(NewMessage)) return;

            var msg = new ChatMessage
            {
                Content    = NewMessage.Trim(),
                SentAt     = DateTime.Now,
                SenderRole = SenderRole,
                PatientId  = PatientId,
                DoctorId   = DoctorId,
                // Mesajul tocmai trimis e intotdeauna al meu → dreapta
                IsSentByMe  = true,
                SenderLabel = string.Empty
            };

            _context.ChatMessages.Add(msg);
            await _context.SaveChangesAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Messages.Add(msg);
                NewMessage = string.Empty;
            });
        }

        [RelayCommand]
        async Task GoBack() => await Shell.Current.GoToAsync("..");
    }
}

using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;

namespace _4Life.Models
{
    public partial class MedicineEntry : ObservableObject
    {
        public Medicine Source { get; set; }
        public string Name     { get; set; }
        public string Dosage   { get; set; }
        public string MealTime { get; set; }

        // True daca e adaugat de pacient (fara doctor prescriitor)
        public bool IsOtc { get; set; }

        public int StockQuantity => Source.StockQuantity;

        [ObservableProperty]
        private bool isTaken;

        public string MealEmoji => MealTime switch
        {
            "Morning" => "🌅",
            "Lunch"   => "☀️",
            "Dinner"  => "🌙",
            _         => "⏰"
        };

        // Badge text mai sugestiv
        public string SourceLabel => IsOtc ? "My Medicine" : "Prescribed";
        public string SourceIcon  => IsOtc ? "🧴" : "💊";
        public Color  SourceColor => IsOtc ? Colors.SlateBlue : Colors.SeaGreen;

        public string PreferenceKey =>
            $"Taken_{Source.Id}_{MealTime}_{DateTime.Today:yyyy-MM-dd}";

        public void NotifyStockChanged() => OnPropertyChanged(nameof(StockQuantity));
    }
}

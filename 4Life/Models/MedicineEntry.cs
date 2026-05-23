using System.ComponentModel.DataAnnotations.Schema;
using CommunityToolkit.Mvvm.ComponentModel;

namespace _4Life.Models
{
    public partial class MedicineEntry : ObservableObject
    {
        public Medicine Source   { get; set; }
        public string   Name     { get; set; }
        public string   MealTime { get; set; }  // "Morning", "Lunch", "Dinner", "General"
        public bool     IsOtc    { get; set; }

        // Doza specifica acestui moment al zilei (in pastile)
        public int Dose => Source.GetDoseForTime(MealTime);

        // Afisaj doza: "1 pill" / "2 pills"
        public string DoseLabel => Dose == 1 ? "1 pill" : $"{Dose} pills";

        public int StockQuantity => Source.StockQuantity;

        [ObservableProperty] private bool isTaken;

        public string MealEmoji => MealTime switch
        {
            "Morning" => "🌅",
            "Lunch"   => "☀️",
            "Dinner"  => "🌙",
            _         => "⏰"
        };

        public string SourceLabel => IsOtc ? "My Medicine" : "Prescribed";
        public string SourceIcon  => IsOtc ? "🧴" : "💊";
        public Color  SourceColor => IsOtc ? Colors.SlateBlue : Colors.SeaGreen;

        // Cheia Preferences unica per medicament + moment + zi
        public string PreferenceKey =>
            $"Taken_{Source.Id}_{MealTime}_{DateTime.Today:yyyy-MM-dd}";

        public void NotifyStockChanged() => OnPropertyChanged(nameof(StockQuantity));
    }
}

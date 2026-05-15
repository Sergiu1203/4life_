using _4Life.ViewModels;

namespace _4Life.Views;

public partial class DailyCheckInPage : ContentPage
{
    public DailyCheckInPage(DailyCheckInViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

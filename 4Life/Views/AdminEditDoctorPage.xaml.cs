using _4Life.ViewModels;

namespace _4Life.Views;

public partial class AdminEditDoctorPage : ContentPage
{
    public AdminEditDoctorPage(AdminEditDoctorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

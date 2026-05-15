using _4Life.ViewModels;

namespace _4Life.Views;

public partial class AddOwnMedicinePage : ContentPage
{
    public AddOwnMedicinePage(AddOwnMedicineViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

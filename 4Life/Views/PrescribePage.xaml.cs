namespace _4Life.Views;

public partial class PrescribePage : ContentPage
{
    public PrescribePage(ViewModels.PrescribeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
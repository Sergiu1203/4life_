using _4Life.ViewModels;

namespace _4Life.Views;

public partial class SelectDoctorsPage : ContentPage
{
    private readonly SelectDoctorsViewModel _viewModel;

    public SelectDoctorsPage(SelectDoctorsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDoctors();
    }
}

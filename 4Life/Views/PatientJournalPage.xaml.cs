using _4Life.ViewModels;

namespace _4Life.Views;

public partial class PatientJournalPage : ContentPage
{
    private readonly PatientJournalViewModel _viewModel;

    public PatientJournalPage(PatientJournalViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadData();
    }
}

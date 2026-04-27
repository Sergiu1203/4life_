using _4Life.ViewModels;

namespace _4Life.Views;

public partial class DashboardPage : ContentPage
{
    private readonly ViewModels.DashboardViewModel _viewModel;

    public DashboardPage(ViewModels.DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Retrieve the User ID we saved in Preferences during Login
        int currentUserId = Preferences.Default.Get("CurrentUserId", 0);

        if (currentUserId > 0)
        {
            // Force the ViewModel to load data for this specific user
            await _viewModel.LoadPatientData(currentUserId);
        }

    }

    private async void OnMedicationCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        // The BindingContext of the CheckBox is the 'Medicine' object
        var checkbox = (CheckBox)sender;
        var med = checkbox.BindingContext as _4Life.Models.Medicine;

        if (med != null && _viewModel != null)
        {
            // Call the command manually
            await _viewModel.ToggleMedicationTakenCommand.ExecuteAsync(med);
        }
    }
}
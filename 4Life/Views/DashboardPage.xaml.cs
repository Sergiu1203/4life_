using _4Life.ViewModels;

namespace _4Life.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        int currentUserId = Preferences.Default.Get("CurrentUserId", 0);
        if (currentUserId > 0)
            await _viewModel.LoadPatientData(currentUserId);
    }

    private async void OnMedicationCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        var checkbox = (CheckBox)sender;
        // BindingContext e acum MedicineEntry, nu Medicine
        var entry = checkbox.BindingContext as _4Life.Models.MedicineEntry;
        if (entry != null && _viewModel != null)
            await _viewModel.ToggleMedicationTakenCommand.ExecuteAsync(entry);
    }
}

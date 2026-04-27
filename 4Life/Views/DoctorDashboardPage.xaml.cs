namespace _4Life.Views;

public partial class DoctorDashboardPage : ContentPage
{
    private readonly ViewModels.DoctorDashboardViewModel _viewModel;

    public DoctorDashboardPage(ViewModels.DoctorDashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // For now, we need to pass the current User ID. 
        // Later, we will use a SecureStorage/Preferences service for this.
        // Assuming we have a way to get the logged in User ID:
        // await _viewModel.LoadPatients(currentUserId);

        int currentUserId = Preferences.Default.Get("CurrentUserId", 0);

        if (currentUserId > 0)
        {
            // Access the ViewModel and call the Load method
            if (BindingContext is ViewModels.DoctorDashboardViewModel vm)
            {
                await vm.LoadPatients(currentUserId);
                await _viewModel.CheckForEmergencies(currentUserId);
            }
        }
    }
}
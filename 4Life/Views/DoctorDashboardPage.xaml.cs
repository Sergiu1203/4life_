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

        int currentUserId = Preferences.Default.Get("CurrentUserId", 0);
        if (currentUserId > 0)
        {
            await _viewModel.LoadPatients(currentUserId);
            await _viewModel.CheckForEmergencies(currentUserId);
            await _viewModel.CheckUnreadMessages(currentUserId); // NOU
        }
    }
}

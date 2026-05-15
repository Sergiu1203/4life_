using _4Life.ViewModels;

namespace _4Life.Views
{
    public partial class AdminDashboardPage : ContentPage
    {
        private readonly AdminDashboardViewModel _viewModel;

        public AdminDashboardPage(AdminDashboardViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_viewModel != null)
                await _viewModel.LoadAllData();
        }
    }
}

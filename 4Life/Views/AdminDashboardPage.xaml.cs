using _4Life.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _4Life.Views
{
    public partial class AdminDashboardPage : ContentPage
    {
        private readonly AdminDashboardViewModel _viewModel;

        public AdminDashboardPage(AdminDashboardViewModel viewModel)
        {
            InitializeComponent();
            // Setăm BindingContext pentru a face legătura cu XAML
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Reîncărcăm lista de pacienți de fiecare dată când pagina apare
            if (_viewModel != null)
            {
                await _viewModel.LoadAllPatients();
            }
        }
    }
}

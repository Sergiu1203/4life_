using _4Life.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _4Life.Views
{
    public partial class AdminEditPatientPage : ContentPage
    {
        public AdminEditPatientPage(AdminEditPatientViewModel viewModel)
        {
            InitializeComponent();
            // Conectăm ViewModel-ul cu Interfața
            BindingContext = viewModel;
        }
    }
}

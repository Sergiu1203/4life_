using _4Life.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _4Life.Views
{
    public partial class SymptomJournalPage : ContentPage
    {
        private readonly SymptomJournalViewModel _viewModel;

        public SymptomJournalPage(SymptomJournalViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (_viewModel != null)
            {
                await _viewModel.LoadJournalData();
            }
        }
    }
}

using _4Life.Views;

namespace _4Life
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("PrescribePage", typeof(PrescribePage));
            Routing.RegisterRoute(nameof(SymptomJournalPage), typeof(SymptomJournalPage));
            Routing.RegisterRoute(nameof(AdminDashboardPage), typeof(AdminDashboardPage));
            Routing.RegisterRoute(nameof(AdminEditPatientPage), typeof(AdminEditPatientPage));
        }
    }
}

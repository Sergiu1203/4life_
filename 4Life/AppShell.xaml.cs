// AppShell.xaml.cs
using _4Life.Views;

namespace _4Life
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("PrescribePage",              typeof(PrescribePage));
            Routing.RegisterRoute(nameof(SymptomJournalPage),   typeof(SymptomJournalPage));
            Routing.RegisterRoute(nameof(AdminDashboardPage),   typeof(AdminDashboardPage));
            Routing.RegisterRoute(nameof(AdminEditPatientPage), typeof(AdminEditPatientPage));
            Routing.RegisterRoute(nameof(ChatPage),             typeof(ChatPage));
            Routing.RegisterRoute(nameof(SelectDoctorsPage),    typeof(SelectDoctorsPage));
            Routing.RegisterRoute(nameof(ForgotPasswordPage),   typeof(ForgotPasswordPage));
            Routing.RegisterRoute(nameof(AddOwnMedicinePage),   typeof(AddOwnMedicinePage));
            Routing.RegisterRoute(nameof(DailyCheckInPage),     typeof(DailyCheckInPage));
            Routing.RegisterRoute(nameof(PatientJournalPage),   typeof(PatientJournalPage));
            // Nou
            Routing.RegisterRoute(nameof(AdminEditDoctorPage),  typeof(AdminEditDoctorPage));
        }
    }
}

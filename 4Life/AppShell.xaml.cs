namespace _4Life
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("PrescribePage", typeof(Views.PrescribePage));
        }
    }
}

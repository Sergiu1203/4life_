using _4Life.Data;
using _4Life.ViewModels;
using _4Life.Views;
using Microsoft.Extensions.Logging;

namespace _4Life
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            builder.Services.AddDbContext<AppDbContext>();

            builder.Services.AddTransient<RegisterViewModel>();
            builder.Services.AddTransient<RegisterPage>();

            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<LoginViewModel>();

            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<DashboardViewModel>();

            builder.Services.AddTransient<DoctorDashboardPage>();
            builder.Services.AddTransient<DoctorDashboardViewModel>();
            builder.Services.AddTransient<PrescribePage>();
            builder.Services.AddTransient<PrescribeViewModel>();

            builder.Services.AddTransient<SymptomJournalPage>();
            builder.Services.AddTransient<SymptomJournalViewModel>();

            builder.Services.AddTransient<AdminDashboardPage>();
            builder.Services.AddTransient<AdminDashboardViewModel>();
            builder.Services.AddTransient<AdminEditPatientPage>();
            builder.Services.AddTransient<AdminEditPatientViewModel>();

            return builder.Build();
        }
    }
}

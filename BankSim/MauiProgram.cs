using BankSim.Data;
using BankSim.Services;
using BankSim.ViewModels;
using BankSim.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BankSim;

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

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "banksim.db3");
        builder.Services.AddDbContextFactory<BankDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));

        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<BankService>();
        builder.Services.AddSingleton<StatsService>();

        builder.Services.AddTransient<TransferViewModel>();
        builder.Services.AddTransient<TransferPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Crée la base au premier lancement
        using (var scope = app.Services.CreateScope())
        {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BankDbContext>>();
            using var db = factory.CreateDbContext();
            db.Database.EnsureCreated();
        }

        return app;
    }
}
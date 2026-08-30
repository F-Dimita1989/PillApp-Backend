using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PillApp.Api.Data;
using PillApp.Api.Models;

namespace PillApp.Api.Tests;

public class PillAppWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();
    private bool _seeded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // L'applicazione richiede queste impostazioni all'avvio. I test le forniscono qui
        // invece di aggiungere un ambiente "Testing" dentro il codice di produzione.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SupabaseDb"] = "Host=localhost;Database=pillapp_tests",
                ["Security:KeepaliveSecret"] = TestKeepaliveSecret
            });
        });

        builder.ConfigureServices(services =>
        {
            // Va rimossa ogni registrazione legata alle options di AppDbContext: se resta
            // quella con Npgsql, EF Core rifiuta di avere due provider sullo stesso contesto.
            var optionsRegistrations = services
                .Where(descriptor => descriptor.ServiceType.FullName?.Contains("DbContextOptions") == true)
                .ToList();

            foreach (var registration in optionsRegistrations)
                services.Remove(registration);

            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }

    public const string TestKeepaliveSecret = "test-keepalive";

    public async Task EnsureSeededAsync()
    {
        if (_seeded)
            return;

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (!await db.FarmaciClasseA.AnyAsync())
        {
            var now = DateTimeOffset.UtcNow;

            for (var i = 1; i <= 25; i++)
            {
                db.FarmaciClasseA.Add(new FarmacoClasseA
                {
                    Id = i,
                    Aic = $"0230760{i:D2}",
                    DenominazioneConfezione = $"Farmaco Test {i:D2}",
                    PrincipioAttivo = "Paracetamolo",
                    DescrizioneGruppo = "Analgesici",
                    PrezzoPubblico = 5.50m + i,
                    InListaTrasparenzaAifa = true,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            await db.SaveChangesAsync();
        }

        _seeded = true;
    }
}

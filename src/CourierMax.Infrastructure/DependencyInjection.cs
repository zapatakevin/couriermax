using CourierMax.Application.Abstractions;
using CourierMax.Application.Services;
using CourierMax.Infrastructure.Persistence;
using CourierMax.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CourierMax.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCourierMaxInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var conn = configuration.GetConnectionString("CourierMax")
                   ?? "Data Source=couriermax.db";

        services.AddSingleton<KeepAliveSqliteConnection>(sp =>
            new KeepAliveSqliteConnection(conn));

        services.AddDbContext<CourierMaxDbContext>((sp, opt) =>
        {
            // Para ":memory:" compartimos una conexión singleton para que EnsureCreated
            // persista entre scopes de EF. Para disco usamos SQLite normal.
            if (conn.Contains(":memory:", StringComparison.OrdinalIgnoreCase))
            {
                var keepAlive = sp.GetRequiredService<KeepAliveSqliteConnection>();
                opt.UseSqlite(keepAlive.Connection);
            }
            else
            {
                opt.UseSqlite(conn);
            }
        });

        services.AddScoped<IShipmentRepository, ShipmentRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IDriverRepository, DriverRepository>();
        services.AddScoped<ICityRepository, CityRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddScoped<ITariffCalculator, TariffCalculator>();
        services.AddScoped<IBusinessDayCalculator, ColombianBusinessDayCalculator>();
        services.AddSingleton<IClock, SystemClock>();

        services.AddScoped<IReferenceDataService, ReferenceDataService>();
        services.AddScoped<IShipmentService, ShipmentService>();

        return services;
    }

    public static async Task SeedAsync(IServiceProvider provider, CancellationToken ct = default)
    {
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CourierMaxDbContext>();
        await db.Database.EnsureCreatedAsync(ct);
        await ReferenceDataSeeder.SeedAsync(db, ct);
    }
}

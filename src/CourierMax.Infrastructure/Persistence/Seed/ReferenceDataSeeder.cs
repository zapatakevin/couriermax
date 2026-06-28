using CourierMax.Domain.Entities;

namespace CourierMax.Infrastructure.Persistence.Seed;

/// <summary>
/// Siembra ciudades, distancias y flota inicial en línea con los Datos de Referencia del enunciado.
/// Idempotente: si ya hay datos, no inserta.
/// </summary>
public static class ReferenceDataSeeder
{
    public static async Task SeedAsync(CourierMaxDbContext db, CancellationToken ct = default)
    {
        if (db.Cities.Any()) return;

        var bog = new City("Bogotá", "Cundinamarca");
        var med = new City("Medellín", "Antioquia");
        var cal = new City("Cali", "Valle del Cauca");
        var baq = new City("Barranquilla", "Atlántico");
        await db.Cities.AddRangeAsync(bog, med, cal, baq);
        await db.SaveChangesAsync(ct);

        var distances = new (string From, string To, decimal Km, decimal Fee)[]
        {
            ("Bogotá", "Medellín", 480, 12_000m),
            ("Bogotá", "Cali", 360, 9_000m),
            ("Bogotá", "Barranquilla", 950, 20_000m),
            ("Medellín", "Cali", 310, 8_000m),
            ("Medellín", "Barranquilla", 650, 15_000m),
            ("Cali", "Barranquilla", 900, 18_000m),
        };

        var byName = db.Cities.Local.ToDictionary(c => c.Name, c => c);
        foreach (var d in distances)
        {
            await db.CityDistances.AddAsync(new CityDistance(byName[d.From].Id, byName[d.To].Id, d.Km, d.Fee), ct);
        }
        await db.SaveChangesAsync(ct);

        // Vehículos y conductores (Datos de Referencia)
        var juan = new Driver("Juan Pérez", "CC-1001");
        var maria = new Driver("María López", "CC-1002");
        var carlos = new Driver("Carlos Ruiz", "CC-1003");
        await db.Drivers.AddRangeAsync(juan, maria, carlos);
        await db.SaveChangesAsync(ct);

        var v1 = new Vehicle("ABC-123", 500, 10);
        var v2 = new Vehicle("DEF-456", 300, 6);
        var v3 = new Vehicle("GHI-789", 800, 15);
        await db.Vehicles.AddRangeAsync(v1, v2, v3);
        await db.SaveChangesAsync(ct);

        juan.AssignToVehicle(v1.Id); v1.AssignDriver(juan.Id);
        maria.AssignToVehicle(v2.Id); v2.AssignDriver(maria.Id);
        carlos.AssignToVehicle(v3.Id); v3.AssignDriver(carlos.Id);

        db.Drivers.UpdateRange(juan, maria, carlos);
        db.Vehicles.UpdateRange(v1, v2, v3);
        await db.SaveChangesAsync(ct);
    }
}

using CourierMax.Application.Abstractions;
using CourierMax.Application.DTOs.Shipments;
using CourierMax.Application.Services;
using CourierMax.Domain.Entities;
using CourierMax.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CourierMaxTests;

/// <summary>
/// Tests unitarios del servicio de aplicación con repositorios in-memory.
/// Cubre los flujos de negocio principales (RF-01..RF-04, RN-03, RN-04).
/// </summary>
public class ShipmentServiceTests
{
    private readonly TestClock _clock = new();
    private (ShipmentService svc, InMemoryShipmentRepo ships, InMemoryVehicleRepo vehicles,
             InMemoryDriverRepo drivers, InMemoryCityRepo cities) Build()
    {
        var ships = new InMemoryShipmentRepo();
        var vehicles = new InMemoryVehicleRepo();
        var drivers = new InMemoryDriverRepo();
        var cities = new InMemoryCityRepo();
        var uow = new InMemoryUow();
        var tariff = new TariffCalculator();
        var bd = new ColombianBusinessDayCalculator();
        var svc = new ShipmentService(ships, vehicles, drivers, cities, uow, tariff, bd, _clock, NullLogger<ShipmentService>.Instance);
        return (svc, ships, vehicles, drivers, cities);
    }

    private static CreateShipmentRequest ValidRequest() => new(
        Sender: new("Juan Pérez", "3105551234", "Calle 100 #15-20, Bogotá"),
        Recipient: new("Ana López", "6015554321", "Carrera 50 #30-10, Medellín"),
        WeightKg: 5m,
        LengthCm: 30, WidthCm: 30, HeightCm: 30,
        PackageType: PackageType.Fragil,
        ServiceType: ServiceType.Express,
        OriginCity: "Bogotá",
        DestinationCity: "Medellín");

    [Fact]
    public async Task Create_assigns_tracking_code_and_persists()
    {
        var (svc, ships, _, _, cities) = Build();
        await cities.SeedAsync();
        var resp = await svc.CreateAsync(ValidRequest(), "operator");
        resp.TrackingCode.Should().MatchRegex("^CM-\\d{8}$");
        resp.Status.Should().Be(ShipmentStatus.CREADO);
        resp.TotalFee.Should().Be(40_950m); // ejemplo del enunciado
        ships.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Create_with_unknown_city_throws_business_exception()
    {
        var (svc, _, _, _, cities) = Build();
        await cities.SeedAsync();
        var bad = ValidRequest() with { DestinationCity = "Marte" };
        var act = async () => await svc.CreateAsync(bad, "operator");
        await act.Should().ThrowAsync<CourierMax.Domain.Exceptions.BusinessRuleException>()
            .WithMessage("*Marte*");
    }

    [Fact]
    public async Task Create_with_invalid_phone_returns_validation_error()
    {
        var (svc, _, _, _, cities) = Build();
        await cities.SeedAsync();
        var bad = ValidRequest() with { Recipient = ValidRequest().Recipient with { Phone = "123" } };
        var act = async () => await svc.CreateAsync(bad, "operator");
        await act.Should().ThrowAsync<CourierMax.Domain.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task Quote_returns_same_total_as_create_without_persisting()
    {
        var (svc, ships, _, _, cities) = Build();
        await cities.SeedAsync();
        var quote = await svc.QuoteAsync(ValidRequest());
        quote.Total.Should().Be(40_950m);
        ships.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Auto_assign_chooses_lowest_load_vehicle()
    {
        var (svc, _, vehicles, drivers, cities) = Build();
        await cities.SeedAsync();
        await vehicles.SeedAsync();
        await drivers.SeedAsync();
        vehicles.WireDrivers(drivers);
        var resp = await svc.CreateAsync(ValidRequest(), "operator");
        var assigned = await svc.AutoAssignAsync(resp.Id, "operator");
        assigned.AssignedVehicleId.Should().NotBeNull();
        assigned.Status.Should().Be(ShipmentStatus.ASIGNADO);
    }

    [Fact]
    public async Task Assign_to_inactive_vehicle_is_rejected()
    {
        var (svc, _, vehicles, drivers, cities) = Build();
        await cities.SeedAsync();
        await vehicles.SeedAsync();
        await drivers.SeedAsync();
        vehicles.WireDrivers(drivers);
        // desactivar vehículo 1
        var v = (await vehicles.ListActiveAsync(default)).First();
        v.Deactivate();
        vehicles.Update(v);

        var resp = await svc.CreateAsync(ValidRequest(), "operator");
        var act = async () => await svc.AssignAsync(resp.Id, new AssignShipmentRequest(v.Id), "operator");
        await act.Should().ThrowAsync<CourierMax.Domain.Exceptions.BusinessRuleException>().WithMessage("*activo*");
    }

    [Fact]
    public async Task Cancel_releases_assigned_vehicle()
    {
        var (svc, _, vehicles, drivers, cities) = Build();
        await cities.SeedAsync();
        await vehicles.SeedAsync();
        await drivers.SeedAsync();
        vehicles.WireDrivers(drivers);
        var resp = await svc.CreateAsync(ValidRequest(), "operator");
        var assigned = await svc.AutoAssignAsync(resp.Id, "operator");
        assigned.AssignedVehicleId.Should().NotBeNull();
        var cancelled = await svc.CancelAsync(resp.Id, new CancelShipmentRequest("cliente cancela", "operator"), default);
        cancelled.Status.Should().Be(ShipmentStatus.CANCELADO);
        cancelled.AssignedVehicleId.Should().BeNull();
    }

    [Fact]
    public async Task Full_lifecycle_through_service()
    {
        var (svc, _, vehicles, drivers, cities) = Build();
        await cities.SeedAsync();
        await vehicles.SeedAsync();
        await drivers.SeedAsync();
        vehicles.WireDrivers(drivers);

        var resp = await svc.CreateAsync(ValidRequest(), "operator");
        var a = await svc.AutoAssignAsync(resp.Id, "operator");
        var t = await svc.StartTransitAsync(a.Id, new StartTransitRequest("driver"), default);
        var d = await svc.MarkDeliveredAsync(t.Id, new MarkDeliveredRequest("driver"), default);
        d.Status.Should().Be(ShipmentStatus.ENTREGADO);
        d.DeliveredAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Overdue_lists_active_shipments_past_sla()
    {
        var (svc, ships, vehicles, drivers, cities) = Build();
        await cities.SeedAsync();
        await vehicles.SeedAsync();
        await drivers.SeedAsync();
        var resp = await svc.CreateAsync(ValidRequest(), "operator");
        // simular creación hace 10 días hábiles
        var stored = ships.Items.First();
        typeof(Shipment).GetProperty("CreatedAt")!.SetValue(stored, _clock.UtcNow.AddDays(-30));
        var overdue = await svc.ListOverdueAsync(_clock.UtcNow.AddDays(-60), _clock.UtcNow);
        overdue.Should().NotBeEmpty();
        overdue.First().SlaBusinessDays.Should().Be(2);
    }

    [Fact]
    public async Task Driver_metrics_compute_correctly()
    {
        var (svc, _, vehicles, drivers, cities) = Build();
        await cities.SeedAsync();
        await vehicles.SeedAsync();
        await drivers.SeedAsync();
        vehicles.WireDrivers(drivers);

        var resp = await svc.CreateAsync(ValidRequest(), "operator");
        await svc.AutoAssignAsync(resp.Id, "operator");
        await svc.StartTransitAsync(resp.Id, new StartTransitRequest("d"), default);
        await svc.MarkDeliveredAsync(resp.Id, new MarkDeliveredRequest("d"), default);

        var driverId = (await drivers.ListAsync()).First().Id;
        var metrics = await svc.GetDriverMetricsAsync(driverId);
        metrics.TotalDelivered.Should().Be(1);
        metrics.TotalWeightKg.Should().Be(5m);
    }
}

// ----- Test doubles -----

internal sealed class TestClock : IClock
{
    public DateTime UtcNow { get; set; } = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc);
}

internal sealed class InMemoryUow : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
}

internal sealed class InMemoryShipmentRepo : IShipmentRepository
{
    public List<Shipment> Items { get; } = new();
    public Task<Shipment?> GetByIdAsync(long id, CancellationToken ct = default) =>
        Task.FromResult<Shipment?>(Items.FirstOrDefault(s => s.Id == id));
    public Task<Shipment?> GetByTrackingCodeAsync(string code, CancellationToken ct = default) =>
        Task.FromResult<Shipment?>(Items.FirstOrDefault(s => s.TrackingCode == code));
    public Task<bool> ExistsTrackingCodeAsync(string code, CancellationToken ct = default) =>
        Task.FromResult(Items.Any(s => s.TrackingCode == code));
    public Task AddAsync(Shipment s, CancellationToken ct = default)
    {
        typeof(Shipment).GetProperty("Id")!.SetValue(s, Items.Count + 1);
        Items.Add(s);
        return Task.CompletedTask;
    }
    public Task<IReadOnlyList<Shipment>> ListAsync(ShipmentListFilter filter, CancellationToken ct = default)
    {
        IEnumerable<Shipment> q = Items;
        if (filter.Status is ShipmentListStatus.Active)
            q = q.Where(s => s.Status != ShipmentStatus.ENTREGADO && s.Status != ShipmentStatus.CANCELADO);
        else if (filter.Status is ShipmentListStatus.Cancelled)
            q = q.Where(s => s.Status == ShipmentStatus.CANCELADO);
        else if (filter.Status is ShipmentListStatus.Delivered)
            q = q.Where(s => s.Status == ShipmentStatus.ENTREGADO);
        if (filter.DriverId is not null)
            q = q.Where(s => s.AssignedDriverId == filter.DriverId);
        if (filter.FromUtc is not null) q = q.Where(s => s.CreatedAt >= filter.FromUtc);
        if (filter.ToUtc is not null) q = q.Where(s => s.CreatedAt <= filter.ToUtc);
        return Task.FromResult<IReadOnlyList<Shipment>>(q.OrderByDescending(s => s.CreatedAt).ToList());
    }
    public Task<IReadOnlyList<Shipment>> ListOverdueAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Shipment>>(Items
            .Where(s => s.Status != ShipmentStatus.ENTREGADO && s.Status != ShipmentStatus.CANCELADO
                        && s.CreatedAt >= fromUtc && s.CreatedAt <= toUtc)
            .ToList());
    public Task<IReadOnlyList<Shipment>> ListByDriverAsync(long driverId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Shipment>>(Items.Where(s => s.AssignedDriverId == driverId).ToList());
    public void Update(Shipment s) { /* no-op for in-memory */ }
}

internal sealed class InMemoryVehicleRepo : IVehicleRepository
{
    public List<Vehicle> Items { get; } = new();
    private static long _vSeq = 0;
    public async Task SeedAsync()
    {
        if (Items.Any()) return;
        var v1 = new Vehicle("ABC-123", 500, 10); typeof(Entity).GetProperty("Id")!.SetValue(v1, ++_vSeq);
        var v2 = new Vehicle("DEF-456", 300, 6); typeof(Entity).GetProperty("Id")!.SetValue(v2, ++_vSeq);
        var v3 = new Vehicle("GHI-789", 800, 15); typeof(Entity).GetProperty("Id")!.SetValue(v3, ++_vSeq);
        Items.AddRange(new[] { v1, v2, v3 });
        await Task.CompletedTask;
    }
    public Task<Vehicle?> GetByIdAsync(long id, CancellationToken ct = default) =>
        Task.FromResult<Vehicle?>(Items.FirstOrDefault(v => v.Id == id));
    public Task<Vehicle?> GetByIdWithDriverAsync(long id, CancellationToken ct = default) =>
        Task.FromResult<Vehicle?>(Items.FirstOrDefault(v => v.Id == id));
    public async Task<IReadOnlyList<Vehicle>> ListActiveAsync(CancellationToken ct = default) =>
        await Task.FromResult<IReadOnlyList<Vehicle>>(Items.Where(v => v.IsActive).ToList());
    public Task AddAsync(Vehicle v, CancellationToken ct = default) { Items.Add(v); return Task.CompletedTask; }
    public void Update(Vehicle v) { }

    public void WireDrivers(InMemoryDriverRepo drivers)
    {
        var d = drivers.Items;
        if (Items.Count > 0 && d.Count > 0) {
            Items[0].AssignDriver(d[0].Id); d[0].AssignToVehicle(Items[0].Id);
        }
        if (Items.Count > 1 && d.Count > 1) {
            Items[1].AssignDriver(d[1].Id); d[1].AssignToVehicle(Items[1].Id);
        }
        if (Items.Count > 2 && d.Count > 2) {
            Items[2].AssignDriver(d[2].Id); d[2].AssignToVehicle(Items[2].Id);
        }
    }

}

internal sealed class InMemoryDriverRepo : IDriverRepository
{
    public List<Driver> Items { get; } = new();
    private static long _dSeq = 0;
    public async Task SeedAsync()
    {
        if (Items.Any()) return;
        var juan = new Driver("Juan Pérez", "CC-1001"); typeof(Entity).GetProperty("Id")!.SetValue(juan, ++_dSeq);
        var maria = new Driver("María López", "CC-1002"); typeof(Entity).GetProperty("Id")!.SetValue(maria, ++_dSeq);
        var carlos = new Driver("Carlos Ruiz", "CC-1003"); typeof(Entity).GetProperty("Id")!.SetValue(carlos, ++_dSeq);
        Items.AddRange(new[] { juan, maria, carlos });
        await Task.CompletedTask;
    }
    public Task<Driver?> GetByIdAsync(long id, CancellationToken ct = default) =>
        Task.FromResult<Driver?>(Items.FirstOrDefault(d => d.Id == id));
    public async Task<IReadOnlyList<Driver>> ListAsync(CancellationToken ct = default) =>
        await Task.FromResult<IReadOnlyList<Driver>>(Items.ToList());
    public Task AddAsync(Driver d, CancellationToken ct = default) { Items.Add(d); return Task.CompletedTask; }
    public void Update(Driver d) { }
}

internal sealed class InMemoryCityRepo : ICityRepository
{
        private static long _citySeq = 0;
    private static void AssignId(Entity e) => typeof(Entity).GetProperty("Id")!.SetValue(e, ++_citySeq);
    public List<City> Cities { get; } = new();
    public List<CityDistance> Distances { get; } = new();
    public async Task SeedAsync()
    {
        if (Cities.Any()) return;
        var bog = new City("Bogotá", "Cundinamarca");
        var med = new City("Medellín", "Antioquia");
        var cal = new City("Cali", "Valle del Cauca");
        var baq = new City("Barranquilla", "Atlántico");
        AssignId(bog); AssignId(med); AssignId(cal); AssignId(baq);
        Cities.AddRange(new[] { bog, med, cal, baq });
        Distances.Add(new CityDistance(bog.Id, med.Id, 480, 12_000));
        Distances.Add(new CityDistance(bog.Id, cal.Id, 360, 9_000));
        Distances.Add(new CityDistance(bog.Id, baq.Id, 950, 20_000));
        Distances.Add(new CityDistance(med.Id, cal.Id, 310, 8_000));
        Distances.Add(new CityDistance(med.Id, baq.Id, 650, 15_000));
        Distances.Add(new CityDistance(cal.Id, baq.Id, 900, 18_000));
        await Task.CompletedTask;
    }
    public Task<City?> GetByIdAsync(long id, CancellationToken ct = default) =>
        Task.FromResult<City?>(Cities.FirstOrDefault(c => c.Id == id));
    public async Task<IReadOnlyList<City>> ListAsync(CancellationToken ct = default) =>
        await Task.FromResult<IReadOnlyList<City>>(Cities.ToList());
    public async Task<IReadOnlyList<CityDistance>> ListDistancesAsync(CancellationToken ct = default) =>
        await Task.FromResult<IReadOnlyList<CityDistance>>(Distances.ToList());
    public Task AddCityAsync(City c, CancellationToken ct = default) { Cities.Add(c); return Task.CompletedTask; }
    public Task AddDistanceAsync(CityDistance d, CancellationToken ct = default) { Distances.Add(d); return Task.CompletedTask; }

    public async Task<City> RequireCityByNameAsync(string name, CancellationToken ct = default)
    {
        var list = await ListAsync(ct);
        var match = list.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
        return match ?? throw new CourierMax.Domain.Exceptions.BusinessRuleException("CITY_NOT_FOUND", $"La ciudad '{name}' no es válida.");
    }

    public async Task<CityDistance?> GetDistanceAsync(long fromCityId, long toCityId, CancellationToken ct = default)
    {
        var list = await ListDistancesAsync(ct);
        return list.FirstOrDefault(d =>
            (d.FromCityId == fromCityId && d.ToCityId == toCityId) ||
            (d.FromCityId == toCityId && d.ToCityId == fromCityId));
    }
}

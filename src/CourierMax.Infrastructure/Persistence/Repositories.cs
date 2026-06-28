using CourierMax.Application.Abstractions;
using CourierMax.Domain.Entities;
using CourierMax.Domain.Enums;
using CourierMax.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace CourierMax.Infrastructure.Persistence;

public sealed class ShipmentRepository : IShipmentRepository
{
    private readonly CourierMaxDbContext _db;
    public ShipmentRepository(CourierMaxDbContext db) => _db = db;

    public Task<Shipment?> GetByIdAsync(long id, CancellationToken ct = default) =>
        _db.Shipments.Include(x => x.Transitions).FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Shipment?> GetByTrackingCodeAsync(string code, CancellationToken ct = default) =>
        _db.Shipments.Include(x => x.Transitions).FirstOrDefaultAsync(s => s.TrackingCode == code, ct);

    public Task<bool> ExistsTrackingCodeAsync(string code, CancellationToken ct = default) =>
        _db.Shipments.AnyAsync(s => s.TrackingCode == code, ct);

    public async Task AddAsync(Shipment shipment, CancellationToken ct = default)
    {
        await _db.Shipments.AddAsync(shipment, ct);
    }

    public async Task<IReadOnlyList<Shipment>> ListAsync(ShipmentListFilter filter, CancellationToken ct = default)
    {
        IQueryable<Shipment> q = _db.Shipments.Include(s => s.Transitions).AsNoTracking();
        if (filter.Status is ShipmentListStatus.Active)
            q = q.Where(s => s.Status != ShipmentStatus.ENTREGADO && s.Status != ShipmentStatus.CANCELADO);
        else if (filter.Status is ShipmentListStatus.Cancelled)
            q = q.Where(s => s.Status == ShipmentStatus.CANCELADO);
        else if (filter.Status is ShipmentListStatus.Delivered)
            q = q.Where(s => s.Status == ShipmentStatus.ENTREGADO);

        if (filter.DriverId is not null)
            q = q.Where(s => s.AssignedDriverId == filter.DriverId);

        if (filter.FromUtc is not null)
            q = q.Where(s => s.CreatedAt >= filter.FromUtc);
        if (filter.ToUtc is not null)
            q = q.Where(s => s.CreatedAt <= filter.ToUtc);

        return await q.OrderByDescending(s => s.CreatedAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Shipment>> ListOverdueAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        return await _db.Shipments
            .Include(s => s.Transitions)
            .Where(s => s.Status != ShipmentStatus.ENTREGADO
                        && s.Status != ShipmentStatus.CANCELADO
                        && s.CreatedAt >= fromUtc
                        && s.CreatedAt <= toUtc)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Shipment>> ListByDriverAsync(long driverId, CancellationToken ct = default) =>
        await _db.Shipments.Include(s => s.Transitions).Where(s => s.AssignedDriverId == driverId).ToListAsync(ct);

    public void Update(Shipment shipment) => _db.Shipments.Update(shipment);
}

public sealed class VehicleRepository : IVehicleRepository
{
    private readonly CourierMaxDbContext _db;
    public VehicleRepository(CourierMaxDbContext db) => _db = db;

    public Task<Vehicle?> GetByIdAsync(long id, CancellationToken ct = default) =>
        _db.Vehicles.FirstOrDefaultAsync(v => v.Id == id, ct);

    public Task<Vehicle?> GetByIdWithDriverAsync(long id, CancellationToken ct = default) =>
        _db.Vehicles.FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<IReadOnlyList<Vehicle>> ListActiveAsync(CancellationToken ct = default) =>
        await _db.Vehicles.Where(v => v.IsActive).ToListAsync(ct);

    public async Task AddAsync(Vehicle v, CancellationToken ct = default) => await _db.Vehicles.AddAsync(v, ct);
    public void Update(Vehicle v) => _db.Vehicles.Update(v);
}

public sealed class DriverRepository : IDriverRepository
{
    private readonly CourierMaxDbContext _db;
    public DriverRepository(CourierMaxDbContext db) => _db = db;

    public Task<Driver?> GetByIdAsync(long id, CancellationToken ct = default) =>
        _db.Drivers.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IReadOnlyList<Driver>> ListAsync(CancellationToken ct = default) =>
        await _db.Drivers.ToListAsync(ct);

    public async Task AddAsync(Driver d, CancellationToken ct = default) => await _db.Drivers.AddAsync(d, ct);
    public void Update(Driver d) => _db.Drivers.Update(d);
}

public sealed class CityRepository : ICityRepository
{
    private readonly CourierMaxDbContext _db;
    public CityRepository(CourierMaxDbContext db) => _db = db;

    public Task<City?> GetByIdAsync(long id, CancellationToken ct = default) =>
        _db.Cities.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<City>> ListAsync(CancellationToken ct = default) =>
        await _db.Cities.OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<CityDistance>> ListDistancesAsync(CancellationToken ct = default) =>
        await _db.CityDistances.ToListAsync(ct);

    public async Task AddCityAsync(City c, CancellationToken ct = default) => await _db.Cities.AddAsync(c, ct);
    public async Task AddDistanceAsync(CityDistance d, CancellationToken ct = default) => await _db.CityDistances.AddAsync(d, ct);

    public async Task<City> RequireCityByNameAsync(string name, CancellationToken ct = default)
    {
        var list = await ListAsync(ct);
        var match = list.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
        return match ?? throw new BusinessRuleException("CITY_NOT_FOUND", $"La ciudad '{name}' no es válida.");
    }

    public async Task<CityDistance?> GetDistanceAsync(long fromCityId, long toCityId, CancellationToken ct = default)
    {
        var list = await ListDistancesAsync(ct);
        return list.FirstOrDefault(d =>
            (d.FromCityId == fromCityId && d.ToCityId == toCityId) ||
            (d.FromCityId == toCityId && d.ToCityId == fromCityId));
    }
}

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly CourierMaxDbContext _db;
    public EfUnitOfWork(CourierMaxDbContext db) => _db = db;
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

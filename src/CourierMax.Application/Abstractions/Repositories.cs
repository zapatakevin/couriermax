using CourierMax.Domain.Entities;
using CourierMax.Domain.Exceptions;

namespace CourierMax.Application.Abstractions;

/// <summary>Contratos de persistencia. La capa de infraestructura los implementa con EF Core.</summary>
public interface IShipmentRepository
{
    Task<Shipment?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<Shipment?> GetByTrackingCodeAsync(string code, CancellationToken ct = default);
    Task<bool> ExistsTrackingCodeAsync(string code, CancellationToken ct = default);
    Task AddAsync(Shipment shipment, CancellationToken ct = default);
    Task<IReadOnlyList<Shipment>> ListAsync(ShipmentListFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<Shipment>> ListOverdueAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
    Task<IReadOnlyList<Shipment>> ListByDriverAsync(long driverId, CancellationToken ct = default);
    void Update(Shipment shipment);
}

public sealed record ShipmentListFilter(
    ShipmentListStatus? Status = null,
    long? DriverId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null);

public enum ShipmentListStatus { Active, Cancelled, Delivered }

public interface IVehicleRepository
{
    Task<Vehicle?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<Vehicle?> GetByIdWithDriverAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<Vehicle>> ListActiveAsync(CancellationToken ct = default);
    Task AddAsync(Vehicle v, CancellationToken ct = default);
    void Update(Vehicle v);
}

public interface IDriverRepository
{
    Task<Driver?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<Driver>> ListAsync(CancellationToken ct = default);
    Task AddAsync(Driver d, CancellationToken ct = default);
    void Update(Driver d);
}

public interface ICityRepository
{
    Task<City?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<City>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CityDistance>> ListDistancesAsync(CancellationToken ct = default);
    Task AddCityAsync(City c, CancellationToken ct = default);
    Task AddDistanceAsync(CityDistance d, CancellationToken ct = default);

    /// <summary>Lookup por nombre case-insensitive; lanza <see cref="BusinessRuleException"/> si no existe.</summary>
    Task<City> RequireCityByNameAsync(string name, CancellationToken ct = default);

    /// <summary>Distancia/tarifa entre dos ciudades (grafo no dirigido).</summary>
    Task<CityDistance?> GetDistanceAsync(long fromCityId, long toCityId, CancellationToken ct = default);
}

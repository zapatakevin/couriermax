using CourierMax.Application.Abstractions;
using CourierMax.Application.DTOs.Reference;

namespace CourierMax.Application.Services;

public sealed class ReferenceDataService : IReferenceDataService
{
    private readonly ICityRepository _cities;
    private readonly IVehicleRepository _vehicles;
    private readonly IDriverRepository _drivers;

    public ReferenceDataService(ICityRepository cities, IVehicleRepository vehicles, IDriverRepository drivers)
    {
        _cities = cities;
        _vehicles = vehicles;
        _drivers = drivers;
    }

    public async Task<IReadOnlyList<CityDto>> ListCitiesAsync(CancellationToken ct = default)
    {
        var list = await _cities.ListAsync(ct);
        return list.Select(c => new CityDto(c.Id, c.Name, c.Department)).ToList();
    }

    public async Task<IReadOnlyList<CityDistanceDto>> ListDistancesAsync(CancellationToken ct = default)
    {
        var list = await _cities.ListDistancesAsync(ct);
        return list.Select(d => new CityDistanceDto(d.FromCityId, d.ToCityId, d.DistanceKm, d.DistanceFee)).ToList();
    }

    public async Task<IReadOnlyList<VehicleDto>> ListVehiclesAsync(CancellationToken ct = default)
    {
        var list = await _vehicles.ListActiveAsync(ct);
        var drivers = await _drivers.ListAsync(ct);
        return list.Select(v =>
        {
            var drv = v.DriverId is null ? null : drivers.FirstOrDefault(d => d.Id == v.DriverId);
            return new VehicleDto(v.Id, v.Plate, v.MaxWeightKg, v.MaxVolumeM3, v.IsActive, v.DriverId, drv?.FullName);
        }).ToList();
    }

    public async Task<IReadOnlyList<DriverDto>> ListDriversAsync(CancellationToken ct = default)
    {
        var list = await _drivers.ListAsync(ct);
        var vehicles = await _vehicles.ListActiveAsync(ct);
        return list.Select(d =>
        {
            var veh = d.VehicleId is null ? null : vehicles.FirstOrDefault(v => v.Id == d.VehicleId);
            return new DriverDto(d.Id, d.FullName, d.Identification, d.IsActive, d.VehicleId, veh?.Plate);
        }).ToList();
    }
}

using CourierMax.Application.DTOs.Reference;
using CourierMax.Domain.Entities;

namespace CourierMax.Application.Services;

public interface IReferenceDataService
{
    Task<IReadOnlyList<CityDto>> ListCitiesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CityDistanceDto>> ListDistancesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<VehicleDto>> ListVehiclesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DriverDto>> ListDriversAsync(CancellationToken ct = default);
}

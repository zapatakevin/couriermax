namespace CourierMax.Application.DTOs.Reference;

public sealed record CityDto(long Id, string Name, string Department);

public sealed record CityDistanceDto(long FromCityId, long ToCityId, decimal Km, decimal Fee);

public sealed record VehicleDto(long Id, string Plate, decimal MaxWeightKg, decimal MaxVolumeM3, bool IsActive, long? DriverId, string? DriverName);

public sealed record DriverDto(long Id, string FullName, string Identification, bool IsActive, long? VehicleId, string? VehiclePlate);

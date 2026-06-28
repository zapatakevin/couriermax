using CourierMax.Domain.Exceptions;

namespace CourierMax.Domain.Entities;

/// <summary>
/// Vehículo de la flota con capacidad máxima de peso (kg) y volumen (m³).
/// La capacidad se evalúa en tiempo real al asignar (RN-01).
/// </summary>
public sealed class Vehicle : Entity
{
    public string Plate { get; private set; } = default!;
    public decimal MaxWeightKg { get; private set; }
    public decimal MaxVolumeM3 { get; private set; }
    public bool IsActive { get; private set; }
    public long? DriverId { get; private set; }

    private Vehicle() { }

    public Vehicle(string plate, decimal maxWeightKg, decimal maxVolumeM3, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(plate)) throw new ValidationException(nameof(plate), "Placa obligatoria.");
        if (maxWeightKg <= 0) throw new ValidationException(nameof(maxWeightKg), "Debe ser > 0.");
        if (maxVolumeM3 <= 0) throw new ValidationException(nameof(maxVolumeM3), "Debe ser > 0.");
        Plate = plate.ToUpperInvariant();
        MaxWeightKg = maxWeightKg;
        MaxVolumeM3 = maxVolumeM3;
        IsActive = isActive;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
    public void AssignDriver(long driverId) => DriverId = driverId;
    public void UnassignDriver() => DriverId = null;
}

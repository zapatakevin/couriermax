namespace CourierMax.Domain.Entities;

/// <summary>Conductor. Relación 1:1 con <see cref="Vehicle"/>.</summary>
public sealed class Driver : Entity
{
    public string FullName { get; private set; } = default!;
    public string Identification { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public long? VehicleId { get; private set; }

    private Driver() { }
    public Driver(string fullName, string identification, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(fullName)) throw new Exceptions.ValidationException(nameof(fullName), "Obligatorio.");
        if (string.IsNullOrWhiteSpace(identification)) throw new Exceptions.ValidationException(nameof(identification), "Obligatorio.");
        FullName = fullName;
        Identification = identification;
        IsActive = isActive;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
    public void AssignToVehicle(long vehicleId) => VehicleId = vehicleId;
    public void UnassignVehicle() => VehicleId = null;
}

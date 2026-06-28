namespace CourierMax.Domain.Entities;

/// <summary>
/// Ciudad colombiana válida para orígenes/destinos de envío.
/// </summary>
public sealed class City : Entity
{
    public string Name { get; private set; } = default!;
    public string Department { get; private set; } = default!;

    private City() { }
    public City(string name, string department)
    {
        Name = name;
        Department = department;
    }
}

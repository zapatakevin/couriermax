namespace CourierMax.Domain.Common;

/// <summary>
/// Clase base para todas las entidades del dominio. Garantiza identidad vía Id
/// y provee igualdad por referencia (las entidades no son value objects).
/// </summary>
public abstract class Entity
{
    public long Id { get; protected set; }

    protected Entity() { }

    public override bool Equals(object? obj) => obj is Entity e && e.Id == Id && e.GetType() == GetType();
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}

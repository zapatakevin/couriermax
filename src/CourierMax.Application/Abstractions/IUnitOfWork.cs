namespace CourierMax.Application.Abstractions;

/// <summary>
/// Unit of Work: garantiza que todas las escrituras del agregado
/// persistan juntas o no persistan. (D de SOLID: Inversión de Dependencias).
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

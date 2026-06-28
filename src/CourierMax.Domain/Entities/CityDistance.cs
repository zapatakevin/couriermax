namespace CourierMax.Domain.Entities;

/// <summary>
/// Distancia y tarifa de distancia entre dos ciudades (grafo no dirigido).
/// El sistema solo conoce estas 6 rutas, en línea con los Datos de Referencia.
/// </summary>
public sealed class CityDistance : Entity
{
    public long FromCityId { get; private set; }
    public long ToCityId { get; private set; }
    public decimal DistanceKm { get; private set; }
    public decimal DistanceFee { get; private set; }

    private CityDistance() { }
    public CityDistance(long fromCityId, long toCityId, decimal distanceKm, decimal distanceFee)
    {
        if (distanceKm <= 0) throw new Exceptions.ValidationException(nameof(distanceKm), "Debe ser > 0.");
        if (distanceFee < 0) throw new Exceptions.ValidationException(nameof(distanceFee), "Debe ser >= 0.");
        FromCityId = fromCityId;
        ToCityId = toCityId;
        DistanceKm = distanceKm;
        DistanceFee = distanceFee;
    }
}

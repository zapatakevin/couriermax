using CourierMax.Domain.Entities;
using CourierMax.Domain.Enums;

namespace CourierMax.Application.Services;

/// <summary>
/// Servicio de dominio: calcula el desglose de tarifa de un envío (RF-04).
/// Reglas:
/// - Tarifa base por tipo de servicio
/// - + $1.500 por cada kg por encima de 2 kg
/// - + recargo por distancia según la ruta
/// - + % sobre la suma por tipo de paquete (Frágil 30%, Perecedero 25%)
/// </summary>
public interface ITariffCalculator
{
    TariffBreakdown Calculate(ServiceType service, PackageType package, decimal weightKg, decimal distanceFee);
}

public sealed class TariffCalculator : ITariffCalculator
{
    private const decimal ExtraKgFee = 1_500m;
    private const decimal IncludedKg = 2m;

    private static readonly Dictionary<ServiceType, decimal> BaseFees = new()
    {
        [ServiceType.Estandar] = 8_000m,
        [ServiceType.Express]  = 15_000m,
        [ServiceType.MismoDia] = 25_000m
    };

    private static readonly Dictionary<PackageType, decimal> Surcharges = new()
    {
        [PackageType.Documento] = 0m,
        [PackageType.Paquete]   = 0m,
        [PackageType.Fragil]    = 0.30m,
        [PackageType.Perecedero] = 0.25m
    };

    public TariffBreakdown Calculate(ServiceType service, PackageType package, decimal weightKg, decimal distanceFee)
    {
        var baseFee = BaseFees[service];
        var extraKg = Math.Max(0m, weightKg - IncludedKg);
        var weightFee = Math.Round(extraKg * ExtraKgFee, 2, MidpointRounding.AwayFromZero);
        var distance = Math.Max(0m, distanceFee);
        var subtotal = baseFee + weightFee + distance;
        var pct = Surcharges[package];
        var surcharge = Math.Round(subtotal * pct, 2, MidpointRounding.AwayFromZero);
        var total = Math.Round(subtotal + surcharge, 2, MidpointRounding.AwayFromZero);
        return new TariffBreakdown(baseFee, weightFee, distance, surcharge, total);
    }
}

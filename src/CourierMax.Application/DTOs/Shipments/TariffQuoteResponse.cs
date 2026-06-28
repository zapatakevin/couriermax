namespace CourierMax.Application.DTOs.Shipments;

public sealed record TariffQuoteResponse(
    decimal BaseFee,
    decimal WeightFee,
    decimal DistanceFee,
    decimal PackageSurcharge,
    decimal Total);

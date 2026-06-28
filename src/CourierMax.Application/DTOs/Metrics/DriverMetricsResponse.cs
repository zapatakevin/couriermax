namespace CourierMax.Application.DTOs.Metrics;

public sealed record DriverMetricsResponse(
    long DriverId,
    string DriverName,
    int TotalAssigned,
    int TotalDelivered,
    int TotalCancelled,
    int TotalInTransit,
    decimal AverageDeliveryDays,
    decimal SlaCompliancePercent,
    decimal TotalWeightKg);

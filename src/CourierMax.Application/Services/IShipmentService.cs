using CourierMax.Application.DTOs.Common;
using CourierMax.Application.DTOs.Metrics;
using CourierMax.Application.DTOs.Shipments;

namespace CourierMax.Application.Services;

public interface IShipmentService
{
    Task<ShipmentResponse> CreateAsync(CreateShipmentRequest req, string actorId, CancellationToken ct = default);
    Task<ShipmentResponse?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ShipmentResponse?> GetByTrackingCodeAsync(string code, CancellationToken ct = default);
    Task<PageResult<ShipmentResponse>> ListAsync(int page, int pageSize, string? status, CancellationToken ct = default);
    Task<ShipmentResponse> AssignAsync(long shipmentId, AssignShipmentRequest req, string actorId, CancellationToken ct = default);
    Task<ShipmentResponse> AutoAssignAsync(long shipmentId, string actorId, CancellationToken ct = default);
    Task<ShipmentResponse> StartTransitAsync(long shipmentId, StartTransitRequest req, CancellationToken ct = default);
    Task<ShipmentResponse> MarkDeliveredAsync(long shipmentId, MarkDeliveredRequest req, CancellationToken ct = default);
    Task<ShipmentResponse> CancelAsync(long shipmentId, CancelShipmentRequest req, CancellationToken ct = default);
    Task<TariffQuoteResponse> QuoteAsync(CreateShipmentRequest req, CancellationToken ct = default);
    Task<IReadOnlyList<OverdueShipmentResponse>> ListOverdueAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
    Task<DriverMetricsResponse> GetDriverMetricsAsync(long driverId, CancellationToken ct = default);
}

using CourierMax.Application.Abstractions;
using CourierMax.Application.DTOs.Common;
using CourierMax.Application.DTOs.Metrics;
using CourierMax.Application.DTOs.Shipments;
using CourierMax.Application.Mappings;
using CourierMax.Domain.Entities;
using CourierMax.Domain.Enums;
using CourierMax.Domain.Exceptions;
using CourierMax.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CourierMax.Application.Services;

/// <summary>
/// Servicio de aplicación que orquesta casos de uso de envíos.
/// Mantiene el dominio libre de dependencias de infraestructura (DIP).
/// </summary>
public sealed class ShipmentService : IShipmentService
{
    private readonly IShipmentRepository _shipments;
    private readonly IVehicleRepository _vehicles;
    private readonly IDriverRepository _drivers;
    private readonly ICityRepository _cities;
    private readonly IUnitOfWork _uow;
    private readonly ITariffCalculator _tariff;
    private readonly IBusinessDayCalculator _bd;
    private readonly IClock _clock;
    private readonly ILogger<ShipmentService> _log;
    private readonly Random _rng = new();

    public ShipmentService(
        IShipmentRepository shipments,
        IVehicleRepository vehicles,
        IDriverRepository drivers,
        ICityRepository cities,
        IUnitOfWork uow,
        ITariffCalculator tariff,
        IBusinessDayCalculator bd,
        IClock clock,
        ILogger<ShipmentService> log)
    {
        _shipments = shipments;
        _vehicles = vehicles;
        _drivers = drivers;
        _cities = cities;
        _uow = uow;
        _tariff = tariff;
        _bd = bd;
        _clock = clock;
        _log = log;
    }

    public async Task<ShipmentResponse> CreateAsync(CreateShipmentRequest req, string actorId, CancellationToken ct = default)
    {
        var origin = await _cities.RequireCityByNameAsync(req.OriginCity, ct);
        var destination = await _cities.RequireCityByNameAsync(req.DestinationCity, ct);

        var distance = await _cities.GetDistanceAsync(origin.Id, destination.Id, ct)
            ?? throw new BusinessRuleException("ROUTE_NOT_FOUND",
                $"No existe tarifa configurada entre {origin.Name} y {destination.Name}.");

        var tariff = _tariff.Calculate(req.ServiceType, req.PackageType, req.WeightKg, distance.DistanceFee);

        // Generar tracking code único
        TrackingCode? code = null;
        for (int i = 0; i < 5 && code is null; i++)
        {
            var candidate = TrackingCode.Generate(_rng);
            if (!await _shipments.ExistsTrackingCodeAsync(candidate.Value, ct))
                code = candidate;
        }
        if (code is null)
            throw new BusinessRuleException("TRACKING_CODE_COLLISION", "No fue posible generar un código único tras 5 intentos.");

        var senderPhone = PhoneNumber.Create(req.Sender.Phone);
        var recipientPhone = PhoneNumber.Create(req.Recipient.Phone);
        var senderAddr = Address.Create(req.Sender.Address);
        var recipientAddr = Address.Create(req.Recipient.Address);
        var dims = PackageDimensions.Create(req.LengthCm, req.WidthCm, req.HeightCm);

        var now = _clock.UtcNow;
        var shipment = new Shipment(
            code,
            req.Sender.Name, senderPhone, senderAddr,
            req.Recipient.Name, recipientPhone, recipientAddr,
            req.WeightKg, dims, req.PackageType, req.ServiceType,
            origin.Id, destination.Id,
            tariff, now);

        await _shipments.AddAsync(shipment, ct);
        await _uow.SaveChangesAsync(ct);
        _log.LogInformation("Envío {Tracking} creado por {Actor}", shipment.TrackingCode, actorId);
        return ToResponse(shipment, origin.Name, destination.Name);
    }

    public async Task<ShipmentResponse?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var s = await _shipments.GetByIdAsync(id, ct);
        if (s is null) return null;
        return await BuildResponseAsync(s, ct);
    }

    public async Task<ShipmentResponse?> GetByTrackingCodeAsync(string code, CancellationToken ct = default)
    {
        var s = await _shipments.GetByTrackingCodeAsync(code, ct);
        if (s is null) return null;
        return await BuildResponseAsync(s, ct);
    }

    public async Task<PageResult<ShipmentResponse>> ListAsync(int page, int pageSize, string? status, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 20;

        ShipmentListStatus? parsed = status?.ToLowerInvariant() switch
        {
            "active" or "activos" => ShipmentListStatus.Active,
            "cancelled" or "cancelados" => ShipmentListStatus.Cancelled,
            "delivered" or "entregados" => ShipmentListStatus.Delivered,
            _ => null
        };

        var all = await _shipments.ListAsync(new ShipmentListFilter(parsed), ct);
        var skip = (page - 1) * pageSize;
        var slice = all.Skip(skip).Take(pageSize).ToList();
        var responses = await Task.WhenAll(slice.Select(s => BuildResponseAsync(s, ct)));
        return new PageResult<ShipmentResponse>(responses, all.Count);
    }

    public async Task<ShipmentResponse> AssignAsync(long shipmentId, AssignShipmentRequest req, string actorId, CancellationToken ct = default)
    {
        var shipment = await _shipments.GetByIdAsync(shipmentId, ct)
            ?? throw new NotFoundException(nameof(Shipment), shipmentId);

        var vehicle = await _vehicles.GetByIdWithDriverAsync(req.VehicleId, ct)
            ?? throw new NotFoundException(nameof(Vehicle), req.VehicleId);
        if (!vehicle.IsActive)
            throw new BusinessRuleException("VEHICLE_INACTIVE", "El vehículo no está activo.");
        if (vehicle.DriverId is null)
            throw new BusinessRuleException("VEHICLE_NO_DRIVER", "El vehículo no tiene conductor asignado.");

        var driver = await _drivers.GetByIdAsync(vehicle.DriverId.Value, ct)
            ?? throw new NotFoundException(nameof(Driver), vehicle.DriverId);
        if (!driver.IsActive)
            throw new BusinessRuleException("DRIVER_INACTIVE", "El conductor no está activo.");

        EnsureCapacity(vehicle, shipment);

        shipment.AssignTo(vehicle.Id, driver.Id, _clock.UtcNow);
        _shipments.Update(shipment);
        await _uow.SaveChangesAsync(ct);
        _log.LogInformation("Envío {Tracking} asignado a {Vehicle}/{Driver}", shipment.TrackingCode, vehicle.Plate, driver.FullName);
        return await BuildResponseAsync(shipment, ct);
    }

    public async Task<ShipmentResponse> AutoAssignAsync(long shipmentId, string actorId, CancellationToken ct = default)
    {
        var shipment = await _shipments.GetByIdAsync(shipmentId, ct)
            ?? throw new NotFoundException(nameof(Shipment), shipmentId);

        var vehicles = await _vehicles.ListActiveAsync(ct);
        // Ordenar por menor carga actual (RN-01: balanceo)
        var ranked = vehicles
            .Where(v => v.DriverId is not null)
            .OrderBy(v => CurrentLoad(v.Id, ct))
            .ToList();

        foreach (var v in ranked)
        {
            try
            {
                EnsureCapacity(v, shipment);
                var driver = await _drivers.GetByIdAsync(v.DriverId!.Value, ct);
                if (driver is null || !driver.IsActive) continue;
                shipment.AssignTo(v.Id, driver.Id, _clock.UtcNow);
                _shipments.Update(shipment);
                await _uow.SaveChangesAsync(ct);
                _log.LogInformation("Envío {Tracking} auto-asignado a {Vehicle}/{Driver}",
                    shipment.TrackingCode, v.Plate, driver.FullName);
                return await BuildResponseAsync(shipment, ct);
            }
            catch (BusinessRuleException)
            {
                // intentar siguiente vehículo
            }
        }
        throw new BusinessRuleException("NO_VEHICLE_AVAILABLE",
            "No hay vehículos activos con capacidad disponible para este envío.");
    }

    public async Task<ShipmentResponse> StartTransitAsync(long shipmentId, StartTransitRequest req, CancellationToken ct = default)
    {
        var shipment = await _shipments.GetByIdAsync(shipmentId, ct)
            ?? throw new NotFoundException(nameof(Shipment), shipmentId);
        shipment.StartTransit(_clock.UtcNow, req.ActorId);
        _shipments.Update(shipment);
        await _uow.SaveChangesAsync(ct);
        return await BuildResponseAsync(shipment, ct);
    }

    public async Task<ShipmentResponse> MarkDeliveredAsync(long shipmentId, MarkDeliveredRequest req, CancellationToken ct = default)
    {
        var shipment = await _shipments.GetByIdAsync(shipmentId, ct)
            ?? throw new NotFoundException(nameof(Shipment), shipmentId);
        shipment.MarkDelivered(_clock.UtcNow, req.ActorId);
        _shipments.Update(shipment);
        await _uow.SaveChangesAsync(ct);
        _log.LogInformation("Envío {Tracking} marcado como ENTREGADO por {Actor}", shipment.TrackingCode, req.ActorId);
        return await BuildResponseAsync(shipment, ct);
    }

    public async Task<ShipmentResponse> CancelAsync(long shipmentId, CancelShipmentRequest req, CancellationToken ct = default)
    {
        var shipment = await _shipments.GetByIdAsync(shipmentId, ct)
            ?? throw new NotFoundException(nameof(Shipment), shipmentId);
        shipment.Cancel(req.Reason, req.ActorId, _clock.UtcNow);
        _shipments.Update(shipment);
        await _uow.SaveChangesAsync(ct);
        _log.LogInformation("Envío {Tracking} cancelado por {Actor}: {Reason}", shipment.TrackingCode, req.ActorId, req.Reason);
        return await BuildResponseAsync(shipment, ct);
    }

    public async Task<TariffQuoteResponse> QuoteAsync(CreateShipmentRequest req, CancellationToken ct = default)
    {
        var origin = await _cities.RequireCityByNameAsync(req.OriginCity, ct);
        var destination = await _cities.RequireCityByNameAsync(req.DestinationCity, ct);
        var distance = await _cities.GetDistanceAsync(origin.Id, destination.Id, ct)
            ?? throw new BusinessRuleException("ROUTE_NOT_FOUND",
                $"No existe tarifa configurada entre {origin.Name} y {destination.Name}.");
        var t = _tariff.Calculate(req.ServiceType, req.PackageType, req.WeightKg, distance.DistanceFee);
        return new TariffQuoteResponse(t.BaseFee, t.WeightFee, t.DistanceFee, t.PackageSurcharge, t.Total);
    }

    public async Task<IReadOnlyList<OverdueShipmentResponse>> ListOverdueAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        if (fromUtc > toUtc) (fromUtc, toUtc) = (toUtc, fromUtc);
        var overdue = await _shipments.ListOverdueAsync(fromUtc, toUtc, ct);
        var result = new List<OverdueShipmentResponse>(overdue.Count);
        foreach (var s in overdue)
        {
            var resp = await BuildResponseAsync(s, ct);
            var sla = SlaBusinessDays(s.ServiceType);
            var bd = _bd.BusinessDaysBetween(s.CreatedAt, _clock.UtcNow);
            result.Add(new OverdueShipmentResponse(resp, bd, sla));
        }
        return result;
    }

    public async Task<DriverMetricsResponse> GetDriverMetricsAsync(long driverId, CancellationToken ct = default)
    {
        var driver = await _drivers.GetByIdAsync(driverId, ct)
            ?? throw new NotFoundException(nameof(Driver), driverId);
        var list = await _shipments.ListByDriverAsync(driverId, ct);

        int delivered = list.Count(s => s.Status == ShipmentStatus.ENTREGADO);
        int cancelled = list.Count(s => s.Status == ShipmentStatus.CANCELADO);
        int inTransit = list.Count(s => s.Status == ShipmentStatus.EN_TRANSITO);
        int total = list.Count;

        var deliveryDurations = list
            .Where(s => s.Status == ShipmentStatus.ENTREGADO && s.AssignedAt is not null && s.DeliveredAt is not null)
            .Select(s => (decimal)_bd.BusinessDaysBetween(s.AssignedAt!.Value, s.DeliveredAt!.Value))
            .ToList();
        var avgDays = deliveryDurations.Count == 0 ? 0m : Math.Round(deliveryDurations.Average(), 2);

        var slaCompliant = list.Count(s => s.Status == ShipmentStatus.ENTREGADO
            && s.AssignedAt is not null && s.DeliveredAt is not null
            && _bd.BusinessDaysBetween(s.AssignedAt.Value, s.DeliveredAt.Value) <= SlaBusinessDays(s.ServiceType));

        var slaPct = delivered == 0 ? 0m : Math.Round((decimal)slaCompliant * 100m / delivered, 2);
        var totalWeight = list.Where(s => s.Status == ShipmentStatus.ENTREGADO).Sum(s => s.WeightKg);

        return new DriverMetricsResponse(driverId, driver.FullName, total, delivered, cancelled, inTransit, avgDays, slaPct, totalWeight);
    }

    // ----- Helpers -----

    private async Task<ShipmentResponse> BuildResponseAsync(Shipment s, CancellationToken ct)
    {
        var origin = await _cities.GetByIdAsync(s.OriginCityId, ct)
            ?? throw new BusinessRuleException("CITY_NOT_FOUND", $"Ciudad origen {s.OriginCityId} no encontrada.");
        var dest = await _cities.GetByIdAsync(s.DestinationCityId, ct)
            ?? throw new BusinessRuleException("CITY_NOT_FOUND", $"Ciudad destino {s.DestinationCityId} no encontrada.");
        return s.ToResponse(origin.Name, dest.Name);
    }

    private ShipmentResponse ToResponse(Shipment s, string origin, string dest) => s.ToResponse(origin, dest);

    private async Task<decimal> CurrentLoad(long vehicleId, CancellationToken ct)
    {
        // Carga actual = suma de pesos de envíos asignados/en tránsito (no ENTREGADO ni CANCELADO).
        var list = (await _shipments.ListAsync(new ShipmentListFilter(), ct))
            .Where(s => s.AssignedVehicleId == vehicleId
                        && s.Status != ShipmentStatus.ENTREGADO
                        && s.Status != ShipmentStatus.CANCELADO)
            .Sum(s => (decimal?)s.WeightKg) ?? 0m;
        return list;
    }

    private static void EnsureCapacity(Vehicle v, Shipment s)
    {
        // Cálculo simple de capacidad: no se aplica aquí el "load actual" porque ya filtra en auto-assign;
        // al asignar manualmente validamos contra la capacidad total.
        if (s.WeightKg > v.MaxWeightKg)
            throw new BusinessRuleException("CAPACITY_EXCEEDED_WEIGHT",
                $"El envío ({s.WeightKg} kg) excede la capacidad máxima de peso del vehículo ({v.MaxWeightKg} kg).");
        if (s.VolumeM3() > v.MaxVolumeM3)
            throw new BusinessRuleException("CAPACITY_EXCEEDED_VOLUME",
                $"El volumen del envío ({s.VolumeM3():F4} m³) excede la capacidad del vehículo ({v.MaxVolumeM3} m³).");
    }

    private static int SlaBusinessDays(Domain.Enums.ServiceType type) => type switch
    {
        Domain.Enums.ServiceType.Estandar => 5,
        Domain.Enums.ServiceType.Express => 2,
        Domain.Enums.ServiceType.MismoDia => 0,
        _ => int.MaxValue
    };
}

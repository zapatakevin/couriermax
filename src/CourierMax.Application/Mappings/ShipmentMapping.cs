using CourierMax.Application.DTOs.Shipments;
using CourierMax.Domain.Entities;

namespace CourierMax.Application.Mappings;

public static class ShipmentMapping
{
    public static ShipmentResponse ToResponse(this Shipment s, string originCity, string destinationCity) =>
        new(
            s.Id,
            s.TrackingCode,
            s.SenderName, s.SenderPhone, s.SenderAddress,
            s.RecipientName, s.RecipientPhone, s.RecipientAddress,
            s.WeightKg, s.Dimensions.Length, s.Dimensions.Width, s.Dimensions.Height,
            s.PackageType, s.ServiceType,
            originCity, destinationCity,
            s.Status,
            s.BaseFee, s.WeightFee, s.DistanceFee, s.PackageSurcharge, s.TotalFee,
            s.AssignedVehicleId, s.AssignedDriverId,
            s.CreatedAt, s.AssignedAt, s.DeliveredAt,
            s.Transitions.Select(t => new TransitionResponse(t.FromStatus, t.ToStatus, t.Reason, t.ActorId, t.ChangedAt)).ToList());
}

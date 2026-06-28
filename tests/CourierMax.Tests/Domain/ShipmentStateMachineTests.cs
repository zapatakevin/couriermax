using CourierMax.Domain.Entities;
using CourierMax.Domain.Enums;
using CourierMax.Domain.Exceptions;
using CourierMax.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace CourierMaxTests;

public class ShipmentStateMachineTests
{
    private static Shipment NewShipment()
    {
        var t = new TariffBreakdown(15_000m, 4_500m, 12_000m, 9_450m, 40_950m);
        return new Shipment(
            TrackingCode.Create("CM-12345678"),
            "Juan", PhoneNumber.Create("3105551234"), Address.Create("Calle 100 #15-20"),
            "Ana", PhoneNumber.Create("6015551234"), Address.Create("Carrera 50 #30-10"),
            5m, PackageDimensions.Create(20, 20, 20),
            PackageType.Fragil, ServiceType.Express,
            1, 2, t, new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void New_shipment_starts_in_CREADO()
    {
        var s = NewShipment();
        s.Status.Should().Be(ShipmentStatus.CREADO);
        s.Transitions.Should().HaveCount(1);
        s.AssignedVehicleId.Should().BeNull();
    }

    [Fact]
    public void Assign_transitions_to_ASIGNADO_and_records_vehicle()
    {
        var s = NewShipment();
        s.AssignTo(10, 5, DateTime.UtcNow);
        s.Status.Should().Be(ShipmentStatus.ASIGNADO);
        s.AssignedVehicleId.Should().Be(10);
        s.AssignedDriverId.Should().Be(5);
        s.Transitions.Should().HaveCount(2);
    }

    [Fact]
    public void Cannot_assign_twice()
    {
        var s = NewShipment();
        s.AssignTo(10, 5, DateTime.UtcNow);
        var act = () => s.AssignTo(11, 6, DateTime.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*ASIGNADO*");
    }

    [Fact]
    public void Full_lifecycle_CREADO_ASIGNADO_TRANSITO_ENTREGADO()
    {
        var s = NewShipment();
        s.AssignTo(10, 5, DateTime.UtcNow);
        s.StartTransit(DateTime.UtcNow, "driver-5");
        s.MarkDelivered(DateTime.UtcNow, "driver-5");
        s.Status.Should().Be(ShipmentStatus.ENTREGADO);
        s.DeliveredAt.Should().NotBeNull();
        s.Transitions.Should().HaveCount(4);
    }

    [Fact]
    public void Cannot_skip_states_from_CREADO_to_TRANSITO()
    {
        var s = NewShipment();
        var act = () => s.StartTransit(DateTime.UtcNow, "x");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Cannot_deliver_without_transit()
    {
        var s = NewShipment();
        s.AssignTo(1, 2, DateTime.UtcNow);
        var act = () => s.MarkDelivered(DateTime.UtcNow, "x");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Cancel_requires_minimum_5_chars_reason()
    {
        var s = NewShipment();
        var act = () => s.Cancel("hi", "x", DateTime.UtcNow);
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Cancel_after_delivered_is_rejected()
    {
        var s = NewShipment();
        s.AssignTo(1, 2, DateTime.UtcNow);
        s.StartTransit(DateTime.UtcNow, "x");
        s.MarkDelivered(DateTime.UtcNow, "x");
        var act = () => s.Cancel("cliente ausente", "x", DateTime.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*entregado*");
    }

    [Fact]
    public void Cancel_releases_vehicle_and_driver()
    {
        var s = NewShipment();
        s.AssignTo(1, 2, DateTime.UtcNow);
        s.Cancel("cliente cancela pedido", "operator", DateTime.UtcNow);
        s.Status.Should().Be(ShipmentStatus.CANCELADO);
        s.AssignedVehicleId.Should().BeNull();
        s.AssignedDriverId.Should().BeNull();
    }
}

using CourierMax.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace CourierMaxTests;

public class ValueObjectsTests
{
    [Theory]
    [InlineData("3105551234", true)]
    [InlineData("6015551234", true)]
    [InlineData("310-555-1234", true)]  // digits extraction
    [InlineData("1234567890", false)]    // no empieza con 3 o 6
    [InlineData("310555123", false)]     // muy corto
    [InlineData("", false)]
    public void Phone_validation(string raw, bool valid)
    {
        var act = () => PhoneNumber.Create(raw);
        if (valid) act.Should().NotThrow();
        else act.Should().Throw<CourierMax.Domain.Exceptions.ValidationException>();
    }

    [Theory]
    [InlineData(0.5, 0.5, 0.5, false)]   // por debajo del mínimo 1cm
    [InlineData(10, 10, 10, true)]
    [InlineData(201, 10, 10, false)]     // excede 200cm
    [InlineData(10, 10, 0, false)]
    public void Package_dimensions_validation(decimal l, decimal w, decimal h, bool valid)
    {
        var act = () => PackageDimensions.Create(l, w, h);
        if (valid) act.Should().NotThrow();
        else act.Should().Throw<CourierMax.Domain.Exceptions.ValidationException>();
    }

    [Fact]
    public void Tracking_code_must_match_format()
    {
        var act = () => TrackingCode.Create("XX-12345678");
        act.Should().Throw<CourierMax.Domain.Exceptions.ValidationException>();
    }

    [Fact]
    public void Tracking_code_generate_produces_well_formed_code()
    {
        var rng = new Random(42);
        var code = TrackingCode.Generate(rng);
        code.Value.Should().MatchRegex("^CM-\\d{8}$");
    }

    [Fact]
    public void Address_min_length_enforced()
    {
        var act = () => Address.Create("x");
        act.Should().Throw<CourierMax.Domain.Exceptions.ValidationException>();
    }

    [Fact]
    public void Weight_range_enforced()
    {
        var t = new CourierMax.Domain.Entities.TariffBreakdown(0, 0, 0, 0, 0);
        var actLow = () => new CourierMax.Domain.Entities.Shipment(
            TrackingCode.Create("CM-12345678"),
            "A", PhoneNumber.Create("3105551234"), Address.Create("Calle 100 #15-20"),
            "B", PhoneNumber.Create("6015551234"), Address.Create("Carrera 50 #30-10"),
            0.05m, PackageDimensions.Create(10, 10, 10),
            CourierMax.Domain.Enums.PackageType.Documento, CourierMax.Domain.Enums.ServiceType.Estandar,
            1, 2, t, DateTime.UtcNow);
        actLow.Should().Throw<CourierMax.Domain.Exceptions.ValidationException>();

        var actHigh = () => new CourierMax.Domain.Entities.Shipment(
            TrackingCode.Create("CM-87654321"),
            "A", PhoneNumber.Create("3105551234"), Address.Create("Calle 100 #15-20"),
            "B", PhoneNumber.Create("6015551234"), Address.Create("Carrera 50 #30-10"),
            150m, PackageDimensions.Create(10, 10, 10),
            CourierMax.Domain.Enums.PackageType.Documento, CourierMax.Domain.Enums.ServiceType.Estandar,
            1, 2, t, DateTime.UtcNow);
        actHigh.Should().Throw<CourierMax.Domain.Exceptions.ValidationException>();
    }
}

using CourierMax.Application.Services;
using CourierMax.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CourierMaxTests;

public class TariffCalculatorTests
{
    private readonly TariffCalculator _calc = new();

    [Theory]
    [InlineData(ServiceType.Estandar, 8_000)]
    [InlineData(ServiceType.Express, 15_000)]
    [InlineData(ServiceType.MismoDia, 25_000)]
    public void Base_fee_matches_table(ServiceType type, decimal expected)
    {
        var t = _calc.Calculate(type, PackageType.Documento, 2m, 0m);
        t.BaseFee.Should().Be(expected);
    }

    [Fact]
    public void Weight_fee_charges_extra_kg_above_2kg()
    {
        // 5kg → 3kg extra × 1500 = 4500
        var t = _calc.Calculate(ServiceType.Express, PackageType.Documento, 5m, 0m);
        t.WeightFee.Should().Be(4_500m);
    }

    [Fact]
    public void Distance_fee_is_added_verbatim()
    {
        var t = _calc.Calculate(ServiceType.Estandar, PackageType.Documento, 2m, 12_000m);
        t.DistanceFee.Should().Be(12_000m);
    }

    [Fact]
    public void Fragil_adds_30_percent_over_subtotal()
    {
        // Bogota-Medellin express fragil 5kg:
        // Base 15000 + Weight 4500 + Dist 12000 = 31500; * 0.30 = 9450
        var t = _calc.Calculate(ServiceType.Express, PackageType.Fragil, 5m, 12_000m);
        t.PackageSurcharge.Should().Be(9_450m);
        t.Total.Should().Be(40_950m);
    }

    [Fact]
    public void Perecedero_adds_25_percent()
    {
        var t = _calc.Calculate(ServiceType.Express, PackageType.Perecedero, 5m, 12_000m);
        t.PackageSurcharge.Should().Be(7_875m);
    }

    [Fact]
    public void Documento_has_no_surcharge()
    {
        var t = _calc.Calculate(ServiceType.Estandar, PackageType.Documento, 1m, 1_000m);
        t.PackageSurcharge.Should().Be(0m);
    }
}

using System.Net;
using System.Net.Http.Json;
using CourierMax.Application.DTOs.Common;
using CourierMax.Application.DTOs.Shipments;
using CourierMax.Application.DTOs.Reference;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CourierMaxTests;

/// <summary>
/// Tests de integración (smoke) usando WebApplicationFactory.
/// Verifican que el pipeline HTTP (controllers + middleware + EF + SQLite in-memory)
/// responde con los códigos correctos.
/// </summary>
public class ShipmentsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ShipmentsApiTests(WebApplicationFactory<Program> factory)
    {
        // Forzar DB en memoria/archivo temporal por test
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:CourierMax", "Data Source=:memory:");
        });
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    [Fact]
    public async Task Health_endpoint_returns_200()
    {
        var client = CreateClient();
        var r = await client.GetAsync("/health");
        r.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Swagger_json_is_exposed()
    {
        var client = CreateClient();
        var r = await client.GetAsync("/swagger/v1/swagger.json");
        r.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reference_cities_returns_seeded_data()
    {
        var client = CreateClient();
        var cities = await client.GetFromJsonAsync<List<CityDto>>("/api/v1/Reference/cities");
        cities.Should().NotBeNull().And.NotBeEmpty();
        cities!.Select(c => c.Name).Should().Contain(new[] { "Bogotá", "Medellín", "Cali", "Barranquilla" });
    }

    [Fact]
    public async Task Create_shipment_returns_201_and_tracking_code()
    {
        var client = CreateClient();
        var req = new CreateShipmentRequest(
            Sender: new("Juan", "3105551234", "Calle 100 #15-20"),
            Recipient: new("Ana", "6015554321", "Carrera 50 #30-10"),
            WeightKg: 5m, LengthCm: 20, WidthCm: 20, HeightCm: 20,
            PackageType: CourierMax.Domain.Enums.PackageType.Fragil,
            ServiceType: CourierMax.Domain.Enums.ServiceType.Express,
            OriginCity: "Bogotá", DestinationCity: "Medellín");
        var r = await client.PostAsJsonAsync("/api/v1/Shipments", req);
        r.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await r.Content.ReadFromJsonAsync<ShipmentResponse>();
        body!.TrackingCode.Should().MatchRegex("^CM-\\d{8}$");
        body.TotalFee.Should().Be(40_950m);
    }

    [Fact]
    public async Task Create_shipment_with_invalid_phone_returns_400()
    {
        var client = CreateClient();
        var req = new CreateShipmentRequest(
            Sender: new("Juan", "123", "Calle 100 #15-20"),
            Recipient: new("Ana", "6015554321", "Carrera 50 #30-10"),
            WeightKg: 5m, LengthCm: 20, WidthCm: 20, HeightCm: 20,
            PackageType: CourierMax.Domain.Enums.PackageType.Fragil,
            ServiceType: CourierMax.Domain.Enums.ServiceType.Express,
            OriginCity: "Bogotá", DestinationCity: "Medellín");
        var r = await client.PostAsJsonAsync("/api/v1/Shipments", req);
        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_shipment_with_unknown_city_returns_409()
    {
        var client = CreateClient();
        var req = new CreateShipmentRequest(
            Sender: new("Juan", "3105551234", "Calle 100 #15-20"),
            Recipient: new("Ana", "6015554321", "Carrera 50 #30-10"),
            WeightKg: 5m, LengthCm: 20, WidthCm: 20, HeightCm: 20,
            PackageType: CourierMax.Domain.Enums.PackageType.Fragil,
            ServiceType: CourierMax.Domain.Enums.ServiceType.Express,
            OriginCity: "Bogotá", DestinationCity: "Marte");
        var r = await client.PostAsJsonAsync("/api/v1/Shipments", req);
        r.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Quote_returns_200_with_breakdown()
    {
        var client = CreateClient();
        var req = new CreateShipmentRequest(
            Sender: new("Juan", "3105551234", "Calle 100 #15-20"),
            Recipient: new("Ana", "6015554321", "Carrera 50 #30-10"),
            WeightKg: 2m, LengthCm: 10, WidthCm: 10, HeightCm: 10,
            PackageType: CourierMax.Domain.Enums.PackageType.Documento,
            ServiceType: CourierMax.Domain.Enums.ServiceType.Estandar,
            OriginCity: "Bogotá", DestinationCity: "Cali");
        var r = await client.PostAsJsonAsync("/api/v1/Shipments/quote", req);
        r.StatusCode.Should().Be(HttpStatusCode.OK);
        var q = await r.Content.ReadFromJsonAsync<TariffQuoteResponse>();
        q!.BaseFee.Should().Be(8_000m);
        q.WeightFee.Should().Be(0m); // 2kg incluidos
        q.DistanceFee.Should().Be(9_000m);
        q.Total.Should().Be(17_000m);
    }
}

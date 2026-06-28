using CourierMax.Application.DTOs.Reference;
using CourierMax.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourierMax.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class ReferenceController : ControllerBase
{
    private readonly IReferenceDataService _svc;
    public ReferenceController(IReferenceDataService svc) => _svc = svc;

    [HttpGet("cities")]
    public async Task<ActionResult<IReadOnlyList<CityDto>>> Cities(CancellationToken ct)
        => Ok(await _svc.ListCitiesAsync(ct));

    [HttpGet("distances")]
    public async Task<ActionResult<IReadOnlyList<CityDistanceDto>>> Distances(CancellationToken ct)
        => Ok(await _svc.ListDistancesAsync(ct));

    [HttpGet("vehicles")]
    public async Task<ActionResult<IReadOnlyList<VehicleDto>>> Vehicles(CancellationToken ct)
        => Ok(await _svc.ListVehiclesAsync(ct));

    [HttpGet("drivers")]
    public async Task<ActionResult<IReadOnlyList<DriverDto>>> Drivers(CancellationToken ct)
        => Ok(await _svc.ListDriversAsync(ct));
}

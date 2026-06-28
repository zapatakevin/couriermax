using CourierMax.Application.DTOs.Metrics;
using CourierMax.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourierMax.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Produces("application/json")]
public sealed class ReportsController : ControllerBase
{
    private readonly IShipmentService _service;
    public ReportsController(IShipmentService service) => _service = service;

    /// <summary>RF-05: Envíos atrasados dentro de un rango.</summary>
    [HttpGet("overdue")]
    [ProducesResponseType(typeof(IReadOnlyList<OverdueShipmentResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OverdueShipmentResponse>>> Overdue(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var f = from ?? DateTime.UtcNow.AddDays(-30);
        var t = to ?? DateTime.UtcNow;
        return Ok(await _service.ListOverdueAsync(f, t, ct));
    }

    /// <summary>RF-06: Métricas de eficiencia por conductor.</summary>
    [HttpGet("drivers/{driverId:long}/metrics")]
    [ProducesResponseType(typeof(DriverMetricsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DriverMetricsResponse>> DriverMetrics([FromRoute] long driverId, CancellationToken ct)
        => Ok(await _service.GetDriverMetricsAsync(driverId, ct));
}

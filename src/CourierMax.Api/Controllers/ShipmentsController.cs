using CourierMax.Application.DTOs.Shipments;
using CourierMax.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourierMax.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class ShipmentsController : ControllerBase
{
    private readonly IShipmentService _service;
    public ShipmentsController(IShipmentService service) => _service = service;

    /// <summary>RF-01: Crea un envío y devuelve el detalle con tarifa calculada.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShipmentResponse>> Create([FromBody] CreateShipmentRequest req, CancellationToken ct)
    {
        var actor = User?.Identity?.Name ?? "system";
        var created = await _service.CreateAsync(req, actor, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Cotiza la tarifa de un envío sin persistirlo.</summary>
    [HttpPost("quote")]
    [ProducesResponseType(typeof(TariffQuoteResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TariffQuoteResponse>> Quote([FromBody] CreateShipmentRequest req, CancellationToken ct)
        => Ok(await _service.QuoteAsync(req, ct));

    /// <summary>Lista envíos con paginación. ?status=active|cancelled|delivered.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? status = null, CancellationToken ct = default)
        => Ok(await _service.ListAsync(page, pageSize, status, ct));

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShipmentResponse>> GetById([FromRoute] long id, CancellationToken ct)
    {
        var s = await _service.GetByIdAsync(id, ct);
        return s is null ? NotFound() : Ok(s);
    }

    [HttpGet("by-code/{code}")]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShipmentResponse>> GetByCode([FromRoute] string code, CancellationToken ct)
    {
        var s = await _service.GetByTrackingCodeAsync(code, ct);
        return s is null ? NotFound() : Ok(s);
    }

    /// <summary>RF-03: Asigna manualmente un envío a un vehículo/conductor.</summary>
    [HttpPost("{id:long}/assign")]
    [ProducesResponseType(typeof(ShipmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ShipmentResponse>> Assign([FromRoute] long id, [FromBody] AssignShipmentRequest req, CancellationToken ct)
    {
        var actor = User?.Identity?.Name ?? "operator";
        return Ok(await _service.AssignAsync(id, req, actor, ct));
    }

    /// <summary>RF-03 + RN-01: Auto-asigna eligiendo el vehículo con menor carga actual.</summary>
    [HttpPost("{id:long}/auto-assign")]
    public async Task<ActionResult<ShipmentResponse>> AutoAssign([FromRoute] long id, CancellationToken ct)
    {
        var actor = User?.Identity?.Name ?? "operator";
        return Ok(await _service.AutoAssignAsync(id, actor, ct));
    }

    /// <summary>RF-02: Inicia tránsito (ASIGNADO → EN_TRANSITO).</summary>
    [HttpPost("{id:long}/start-transit")]
    public async Task<ActionResult<ShipmentResponse>> StartTransit([FromRoute] long id, [FromBody] StartTransitRequest req, CancellationToken ct)
        => Ok(await _service.StartTransitAsync(id, req, ct));

    /// <summary>RF-02: Marca como ENTREGADO.</summary>
    [HttpPost("{id:long}/deliver")]
    public async Task<ActionResult<ShipmentResponse>> Deliver([FromRoute] long id, [FromBody] MarkDeliveredRequest req, CancellationToken ct)
        => Ok(await _service.MarkDeliveredAsync(id, req, ct));

    /// <summary>RF-02/RN-03: Cancela un envío.</summary>
    [HttpPost("{id:long}/cancel")]
    public async Task<ActionResult<ShipmentResponse>> Cancel([FromRoute] long id, [FromBody] CancelShipmentRequest req, CancellationToken ct)
        => Ok(await _service.CancelAsync(id, req, ct));
}

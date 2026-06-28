using System.Net;
using System.Text.Json;
using CourierMax.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace CourierMax.Api.Middleware;

/// <summary>
/// Middleware centralizado para traducir excepciones de dominio a ProblemDetails
/// con códigos HTTP apropiados (KISS: un único punto de traducción).
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _log;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (ValidationException ex)
        {
            await WriteProblemAsync(ctx, HttpStatusCode.BadRequest, "VALIDATION", ex.Message, ex.Errors);
        }
        catch (NotFoundException ex)
        {
            await WriteProblemAsync(ctx, HttpStatusCode.NotFound, ex.Code, ex.Message);
        }
        catch (BusinessRuleException ex)
        {
            await WriteProblemAsync(ctx, HttpStatusCode.Conflict, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unhandled exception");
            await WriteProblemAsync(ctx, HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "Error inesperado del servidor.");
        }
    }

    private static Task WriteProblemAsync(HttpContext ctx, HttpStatusCode status, string code, string message,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        if (ctx.Response.HasStarted) return Task.CompletedTask;
        ctx.Response.Clear();
        ctx.Response.StatusCode = (int)status;
        ctx.Response.ContentType = "application/problem+json";
        var problem = new
        {
            type = $"https://couriermax.com/errors/{code.ToLowerInvariant()}",
            title = code,
            status = (int)status,
            detail = message,
            traceId = ctx.TraceIdentifier,
            errors
        };
        return ctx.Response.WriteAsync(JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}

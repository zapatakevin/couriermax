using CourierMax.Api.Extensions;
using CourierMax.Api.Middleware;
using CourierMax.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Logging (Serilog)
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
builder.Host.UseSerilog();

// Layers
builder.Services.AddCourierMaxInfrastructure(builder.Configuration);

// Web layer
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CourierMax API",
        Version = "v1",
        Description = "API REST para la gestión de envíos de CourierMax (prueba técnica .NET)."
    });
});
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddHealthChecks();

var app = builder.Build();

// Seed inicial
await DependencyInjection.SeedAsync(app.Services);

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CourierMax API v1"));
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }

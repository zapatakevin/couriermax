using CourierMax.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CourierMax.Infrastructure.Persistence;

public sealed class CourierMaxDbContext : DbContext
{
    public CourierMaxDbContext(DbContextOptions<CourierMaxDbContext> options) : base(options) { }

    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<StateTransition> StateTransitions => Set<StateTransition>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<CityDistance> CityDistances => Set<CityDistance>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.ApplyConfigurationsFromAssembly(typeof(CourierMaxDbContext).Assembly);
    }
}

using CourierMax.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CourierMax.Infrastructure.Persistence.Configurations;

public sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> b)
    {
        b.ToTable("Cities");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(80);
        b.Property(x => x.Department).IsRequired().HasMaxLength(80);
        b.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class CityDistanceConfiguration : IEntityTypeConfiguration<CityDistance>
{
    public void Configure(EntityTypeBuilder<CityDistance> b)
    {
        b.ToTable("CityDistances");
        b.HasKey(x => x.Id);
        b.Property(x => x.DistanceKm).HasColumnType("decimal(10,2)");
        b.Property(x => x.DistanceFee).HasColumnType("decimal(12,2)");
        b.HasIndex(x => new { x.FromCityId, x.ToCityId }).IsUnique();
    }
}

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> b)
    {
        b.ToTable("Vehicles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Plate).IsRequired().HasMaxLength(20);
        b.Property(x => x.MaxWeightKg).HasColumnType("decimal(10,2)");
        b.Property(x => x.MaxVolumeM3).HasColumnType("decimal(10,4)");
        b.HasIndex(x => x.Plate).IsUnique();
    }
}

public sealed class DriverConfiguration : IEntityTypeConfiguration<Driver>
{
    public void Configure(EntityTypeBuilder<Driver> b)
    {
        b.ToTable("Drivers");
        b.HasKey(x => x.Id);
        b.Property(x => x.FullName).IsRequired().HasMaxLength(120);
        b.Property(x => x.Identification).IsRequired().HasMaxLength(40);
        b.HasIndex(x => x.Identification).IsUnique();
    }
}

public sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> b)
    {
        b.ToTable("Shipments");
        b.HasKey(x => x.Id);
        b.Property(x => x.TrackingCode).IsRequired().HasMaxLength(20);
        b.HasIndex(x => x.TrackingCode).IsUnique();
        b.Property(x => x.WeightKg).HasColumnType("decimal(10,2)");
        b.OwnsOne(x => x.Dimensions, d =>
        {
            d.Property(p => p.Length).HasColumnName("LengthCm").HasColumnType("decimal(10,2)");
            d.Property(p => p.Width).HasColumnName("WidthCm").HasColumnType("decimal(10,2)");
            d.Property(p => p.Height).HasColumnName("HeightCm").HasColumnType("decimal(10,2)");
        });
        b.Property(x => x.BaseFee).HasColumnType("decimal(12,2)");
        b.Property(x => x.WeightFee).HasColumnType("decimal(12,2)");
        b.Property(x => x.DistanceFee).HasColumnType("decimal(12,2)");
        b.Property(x => x.PackageSurcharge).HasColumnType("decimal(12,2)");
        b.Property(x => x.TotalFee).HasColumnType("decimal(12,2)");

        b.HasMany(x => x.Transitions).WithOne().HasForeignKey("ShipmentId").OnDelete(DeleteBehavior.Cascade);
        // Use shadow FK so we can store transitions without exposing it on the aggregate.
        b.Metadata.FindNavigation(nameof(Shipment.Transitions))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        // Use shadow FK for transitions
        var nav = b.Metadata.FindNavigation(nameof(Shipment.Transitions))!;
        nav.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class StateTransitionConfiguration : IEntityTypeConfiguration<StateTransition>
{
    public void Configure(EntityTypeBuilder<StateTransition> b)
    {
        b.ToTable("StateTransitions");
        b.HasKey(x => x.Id);
        b.Property(x => x.FromStatus).HasConversion<int?>();
        b.Property(x => x.ToStatus).HasConversion<int>();
        b.Property(x => x.Reason).IsRequired().HasMaxLength(500);
        b.Property(x => x.ActorId).IsRequired().HasMaxLength(80);
        // Shipment FK via shadow property
        b.Property<long>("ShipmentId");
    }
}

using GymManager.Domain.BodyMeasurements;
using GymManager.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class BodyMeasurementConfiguration : IEntityTypeConfiguration<BodyMeasurement>
{
    public void Configure(EntityTypeBuilder<BodyMeasurement> builder)
    {
        builder.ToTable("BodyMeasurements");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.HeightCm).HasColumnType("decimal(6,2)");
        builder.Property(b => b.WeightKg).HasColumnType("decimal(6,2)");
        builder.Property(b => b.BodyFatPercentage).HasColumnType("decimal(5,2)");
        builder.Property(b => b.ChestCm).HasColumnType("decimal(6,2)");
        builder.Property(b => b.WaistCm).HasColumnType("decimal(6,2)");
        builder.Property(b => b.HipsCm).HasColumnType("decimal(6,2)");
        builder.Property(b => b.ArmCm).HasColumnType("decimal(6,2)");
        builder.Property(b => b.ThighCm).HasColumnType("decimal(6,2)");

        builder.Property(b => b.Notes).HasMaxLength(2000);
        builder.Property(b => b.PhotoUrl).HasMaxLength(500);

        builder.Ignore(b => b.Bmi);

        builder.HasIndex(b => new { b.MemberId, b.RecordedOnUtc });

        // Shadow (no-navigation) FK — see LeadConfiguration for the rationale.
        builder.HasOne<Member>().WithMany().HasForeignKey(b => b.MemberId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(b => b.RowVersion).IsRowVersion();
    }
}

using GymManager.Domain.Lockers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class LockerConfiguration : IEntityTypeConfiguration<Locker>
{
    public void Configure(EntityTypeBuilder<Locker> builder)
    {
        builder.ToTable("Lockers");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.Number).HasMaxLength(20).IsRequired();
        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(l => l.CreatedBy).HasMaxLength(256);
        builder.Property(l => l.ModifiedBy).HasMaxLength(256);

        builder.HasIndex(l => new { l.BranchId, l.Number }).IsUnique();

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}

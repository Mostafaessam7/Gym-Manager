using GymManager.Domain.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class StaffShiftConfiguration : IEntityTypeConfiguration<StaffShift>
{
    public void Configure(EntityTypeBuilder<StaffShift> builder)
    {
        builder.ToTable("StaffShifts");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Notes).HasMaxLength(1000);

        builder.Property(s => s.CreatedBy).HasMaxLength(256);
        builder.Property(s => s.ModifiedBy).HasMaxLength(256);

        builder.HasIndex(s => new { s.UserId, s.StartUtc });
        builder.HasIndex(s => s.BranchId);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}

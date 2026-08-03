using GymManager.Domain.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.Reason).HasMaxLength(1000);
        builder.Property(l => l.DecisionNotes).HasMaxLength(1000);

        builder.Property(l => l.CreatedBy).HasMaxLength(256);
        builder.Property(l => l.ModifiedBy).HasMaxLength(256);

        builder.HasIndex(l => new { l.UserId, l.StartDate });
        builder.HasIndex(l => l.Status);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}

using GymManager.Domain.Attendance;
using GymManager.Domain.Branches;
using GymManager.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("AttendanceRecords");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Method).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(a => new { a.MemberId, a.CheckOutUtc });
        builder.HasIndex(a => new { a.BranchId, a.CheckInUtc });

        // Shadow (no-navigation) FKs — see LeadConfiguration for the rationale.
        builder.HasOne<Member>().WithMany().HasForeignKey(a => a.MemberId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(a => a.BranchId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(a => a.RowVersion).IsRowVersion();
    }
}

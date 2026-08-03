using GymManager.Domain.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class ClassSessionConfiguration : IEntityTypeConfiguration<ClassSession>
{
    public void Configure(EntityTypeBuilder<ClassSession> builder)
    {
        builder.ToTable("ClassSessions");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.OwnsMany(s => s.Bookings, bookings =>
        {
            bookings.ToTable("ClassBookings");
            bookings.WithOwner().HasForeignKey("ClassSessionId");
            bookings.HasKey(b => b.Id);
            // Client-assigned (Guid.NewGuid() in the domain constructor) — see the comment on
            // UserConfiguration's RefreshTokens/Roles for why this is required.
            bookings.Property(b => b.Id).ValueGeneratedNever();
            bookings.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
            bookings.HasIndex("ClassSessionId", nameof(Domain.Classes.ClassBooking.MemberId));
        });

        builder.Navigation(s => s.Bookings).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(s => s.TrainerId);
        builder.HasIndex(s => s.BranchId);
        builder.HasIndex(s => new { s.StartUtc, s.EndUtc });

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}

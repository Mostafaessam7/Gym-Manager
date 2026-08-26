using GymManager.Domain.Branches;
using GymManager.Domain.Classes;
using GymManager.Domain.Members;
using GymManager.Domain.Trainers;
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

            // Shadow (no-navigation) FK from within this owned type's builder — see LeadConfiguration for
            // the rationale. EF Core supports an owned entity holding an additional relationship to a
            // regular (non-owned) entity like this; it doesn't turn ClassBooking into a shared aggregate root.
            bookings.HasOne<Member>().WithMany().HasForeignKey(b => b.MemberId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Navigation(s => s.Bookings).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(s => s.TrainerId);
        builder.HasIndex(s => s.BranchId);
        builder.HasIndex(s => new { s.StartUtc, s.EndUtc });

        // Shadow (no-navigation) FKs — see LeadConfiguration for the rationale. ClassBooking.MemberId (an
        // owned collection referencing another aggregate) is left index-only, consistent with Phase 11's
        // original scoping — a shadow FK from an owned type still needs its own careful verification and
        // wasn't bundled into this mechanical sweep of top-level entity references.
        builder.HasOne<Trainer>().WithMany().HasForeignKey(s => s.TrainerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(s => s.BranchId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}

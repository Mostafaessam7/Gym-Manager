using GymManager.Domain.Branches;
using GymManager.Domain.Crm;
using GymManager.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("Leads");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.Name).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Email).HasMaxLength(256);
        builder.Property(l => l.Phone).HasMaxLength(30);
        builder.Property(l => l.Notes).HasMaxLength(2000);
        builder.Property(l => l.LostReason).HasMaxLength(500);

        builder.Property(l => l.Source).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.Stage).HasConversion<string>().HasMaxLength(20);

        builder.Property(l => l.CreatedBy).HasMaxLength(256);
        builder.Property(l => l.ModifiedBy).HasMaxLength(256);

        builder.OwnsMany(l => l.FollowUps, followUp =>
        {
            followUp.ToTable("LeadFollowUps");
            followUp.WithOwner().HasForeignKey("LeadId");
            followUp.HasKey(f => f.Id);
            followUp.Property(f => f.Id).ValueGeneratedNever();

            followUp.Property(f => f.Type).HasConversion<string>().HasMaxLength(20);
            followUp.Property(f => f.Notes).HasMaxLength(1000);
        });

        builder.HasIndex(l => l.Stage);
        builder.HasIndex(l => l.AssignedToUserId);
        builder.HasIndex(l => l.BranchId);

        // Shadow (no-navigation) FKs: Lead references Branch/User by id only, matching this codebase's
        // aggregate-boundary convention of never exposing a cross-aggregate navigation property — this adds
        // only the physical database constraint, not a domain-model relationship. Restrict, not Cascade:
        // neither Branch nor User is ever hard-deleted by this application (only deactivated), so this
        // exists purely as a safety net against orphaned rows, not a behavior the app relies on triggering.
        builder.HasOne<Branch>().WithMany().HasForeignKey(l => l.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(l => l.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}

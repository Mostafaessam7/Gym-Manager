using GymManager.Domain.Members;
using GymManager.Domain.Memberships;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("Memberships");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.PlanNameSnapshot).HasMaxLength(150).IsRequired();
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(m => m.CreatedBy).HasMaxLength(256);
        builder.Property(m => m.ModifiedBy).HasMaxLength(256);

        builder.OwnsOne(m => m.PricePaid, price =>
        {
            price.Property(p => p.Amount).HasColumnName("PricePaid").HasColumnType("decimal(18,2)").IsRequired();
            price.Property(p => p.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        builder.Navigation(m => m.PricePaid).IsRequired();

        builder.OwnsMany(m => m.Renewals, renewals =>
        {
            renewals.ToTable("MembershipRenewals");
            renewals.WithOwner().HasForeignKey("MembershipId");
            renewals.HasKey(r => r.Id);
            // Client-assigned (Guid.NewGuid() in the domain constructor) — see the comment on
            // UserConfiguration's RefreshTokens/Roles for why this is required.
            renewals.Property(r => r.Id).ValueGeneratedNever();

            renewals.OwnsOne(r => r.AmountPaid, price =>
            {
                price.Property(p => p.Amount).HasColumnName("AmountPaid").HasColumnType("decimal(18,2)").IsRequired();
                price.Property(p => p.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
            });

            renewals.Navigation(r => r.AmountPaid).IsRequired();
        });

        builder.Navigation(m => m.Renewals).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(m => m.MemberId);
        builder.HasIndex(m => new { m.Status, m.EndDate });

        // Shadow (no-navigation) FK — see LeadConfiguration for the rationale.
        builder.HasOne<Member>().WithMany().HasForeignKey(m => m.MemberId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(m => m.RowVersion).IsRowVersion();
    }
}

using GymManager.Domain.Memberships;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class MembershipPlanConfiguration : IEntityTypeConfiguration<MembershipPlan>
{
    public void Configure(EntityTypeBuilder<MembershipPlan> builder)
    {
        builder.ToTable("MembershipPlans");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).HasMaxLength(150).IsRequired();
        builder.HasIndex(p => p.Name).IsUnique();

        builder.Property(p => p.Description).HasMaxLength(1000);

        builder.Property(p => p.CreatedBy).HasMaxLength(256);
        builder.Property(p => p.ModifiedBy).HasMaxLength(256);
        builder.Property(p => p.DeletedBy).HasMaxLength(256);

        builder.OwnsOne(p => p.Price, price =>
        {
            price.Property(m => m.Amount).HasColumnName("Price").HasColumnType("decimal(18,2)").IsRequired();
            price.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        builder.Navigation(p => p.Price).IsRequired();

        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}

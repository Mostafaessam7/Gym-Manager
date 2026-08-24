using GymManager.Domain.Branches;
using GymManager.Domain.Members;
using GymManager.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Method).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.ReferenceType).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.GatewayProvider).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.GatewayReferenceId).HasMaxLength(255);

        builder.Property(p => p.CreatedBy).HasMaxLength(256);
        builder.Property(p => p.ModifiedBy).HasMaxLength(256);

        builder.OwnsOne(p => p.Amount, amount =>
        {
            amount.Property(m => m.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)").IsRequired();
            amount.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        builder.Navigation(p => p.Amount).IsRequired();

        builder.HasIndex(p => p.MemberId);
        builder.HasIndex(p => p.BranchId);
        builder.HasIndex(p => new { p.ReferenceType, p.ReferenceId });
        builder.HasIndex(p => p.GatewayReferenceId).IsUnique().HasFilter("[GatewayReferenceId] IS NOT NULL");

        // Shadow (no-navigation) FKs — see LeadConfiguration for the rationale.
        builder.HasOne<Member>().WithMany().HasForeignKey(p => p.MemberId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(p => p.BranchId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}

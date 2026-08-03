using GymManager.Domain.Staff;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class CommissionConfiguration : IEntityTypeConfiguration<Commission>
{
    public void Configure(EntityTypeBuilder<Commission> builder)
    {
        builder.ToTable("Commissions");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.SourceType).HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Notes).HasMaxLength(1000);

        builder.Property(c => c.CreatedBy).HasMaxLength(256);
        builder.Property(c => c.ModifiedBy).HasMaxLength(256);

        builder.OwnsOne(c => c.Amount, amount =>
        {
            amount.Property(m => m.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)").IsRequired();
            amount.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });
        builder.Navigation(c => c.Amount).IsRequired();

        builder.HasIndex(c => new { c.UserId, c.Status });

        builder.Property(c => c.RowVersion).IsRowVersion();
    }
}

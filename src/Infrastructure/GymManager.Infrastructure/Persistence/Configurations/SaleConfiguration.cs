using GymManager.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(s => s.CreatedBy).HasMaxLength(256);
        builder.Property(s => s.ModifiedBy).HasMaxLength(256);

        builder.Ignore(s => s.TotalAmount);
        builder.Ignore(s => s.Currency);

        builder.OwnsMany(s => s.Lines, lines =>
        {
            lines.ToTable("SaleLines");
            lines.WithOwner().HasForeignKey("SaleId");
            lines.HasKey(l => l.Id);
            // Client-assigned (Guid.NewGuid() in the domain constructor) — see the comment on
            // UserConfiguration's RefreshTokens/Roles for why this is required.
            lines.Property(l => l.Id).ValueGeneratedNever();
            lines.Property(l => l.ProductNameSnapshot).HasMaxLength(150).IsRequired();

            lines.OwnsOne(l => l.UnitPrice, price =>
            {
                price.Property(p => p.Amount).HasColumnName("UnitPrice").HasColumnType("decimal(18,2)").IsRequired();
                price.Property(p => p.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
            });

            lines.Navigation(l => l.UnitPrice).IsRequired();
            lines.Ignore(l => l.LineTotal);
            lines.Ignore(l => l.RemainingQuantity);
            lines.Ignore(l => l.RefundTotal);
        });

        builder.Navigation(s => s.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(s => s.Payments, payments =>
        {
            payments.ToTable("SalePayments");
            payments.WithOwner().HasForeignKey("SaleId");
            payments.HasKey(p => p.Id);
            payments.Property(p => p.Id).ValueGeneratedNever();

            payments.Property(p => p.Method).HasConversion<string>().HasMaxLength(20);

            payments.OwnsOne(p => p.Amount, amount =>
            {
                amount.Property(m => m.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)").IsRequired();
                amount.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
            });

            payments.Navigation(p => p.Amount).IsRequired();
        });

        builder.Navigation(s => s.Payments).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(s => s.BranchId);
        builder.HasIndex(s => s.MemberId);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}

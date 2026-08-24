using GymManager.Domain.Branches;
using GymManager.Domain.Invoices;
using GymManager.Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.InvoiceNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();

        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(i => i.CreatedBy).HasMaxLength(256);
        builder.Property(i => i.ModifiedBy).HasMaxLength(256);

        builder.Ignore(i => i.TotalAmount);
        builder.Ignore(i => i.Currency);

        builder.OwnsMany(i => i.Lines, lines =>
        {
            lines.ToTable("InvoiceLines");
            lines.WithOwner().HasForeignKey("InvoiceId");
            lines.HasKey(l => l.Id);
            // Client-assigned (Guid.NewGuid() in the domain constructor) — see the comment on
            // UserConfiguration's RefreshTokens/Roles for why this is required.
            lines.Property(l => l.Id).ValueGeneratedNever();
            lines.Property(l => l.Description).HasMaxLength(300).IsRequired();

            lines.OwnsOne(l => l.UnitPrice, price =>
            {
                price.Property(p => p.Amount).HasColumnName("UnitPrice").HasColumnType("decimal(18,2)").IsRequired();
                price.Property(p => p.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
            });

            lines.Navigation(l => l.UnitPrice).IsRequired();
            lines.Ignore(l => l.LineTotal);
        });

        builder.Navigation(i => i.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(i => i.MemberId);
        builder.HasIndex(i => i.BranchId);

        // Shadow (no-navigation) FKs — see LeadConfiguration for the rationale.
        builder.HasOne<Member>().WithMany().HasForeignKey(i => i.MemberId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(i => i.BranchId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(i => i.RowVersion).IsRowVersion();
    }
}

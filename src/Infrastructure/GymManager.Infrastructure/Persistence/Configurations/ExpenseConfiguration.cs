using GymManager.Domain.Branches;
using GymManager.Domain.Expenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("Expenses");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Category).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();
        builder.Property(e => e.PaidTo).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ReceiptUrl).HasMaxLength(500);

        builder.Property(e => e.CreatedBy).HasMaxLength(256);
        builder.Property(e => e.ModifiedBy).HasMaxLength(256);
        builder.Property(e => e.DeletedBy).HasMaxLength(256);

        builder.OwnsOne(e => e.Amount, amount =>
        {
            amount.Property(m => m.Amount).HasColumnName("Amount").HasColumnType("decimal(18,2)").IsRequired();
            amount.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        builder.Navigation(e => e.Amount).IsRequired();

        builder.HasIndex(e => e.BranchId);
        builder.HasIndex(e => e.ExpenseDate);

        // Shadow (no-navigation) FK — see LeadConfiguration for the rationale.
        builder.HasOne<Branch>().WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}

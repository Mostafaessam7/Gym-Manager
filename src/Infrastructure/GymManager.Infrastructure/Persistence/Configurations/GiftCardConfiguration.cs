using GymManager.Domain.GiftCards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class GiftCardConfiguration : IEntityTypeConfiguration<GiftCard>
{
    public void Configure(EntityTypeBuilder<GiftCard> builder)
    {
        builder.ToTable("GiftCards");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();

        builder.Property(g => g.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(g => g.Code).IsUnique();

        builder.OwnsOne(g => g.InitialBalance, money =>
        {
            money.Property(m => m.Amount).HasColumnName("InitialBalanceAmount").HasColumnType("decimal(10,2)").IsRequired();
            money.Property(m => m.Currency).HasColumnName("InitialBalanceCurrency").HasMaxLength(3).IsRequired();
        });
        builder.Navigation(g => g.InitialBalance).IsRequired();

        builder.OwnsOne(g => g.CurrentBalance, money =>
        {
            money.Property(m => m.Amount).HasColumnName("CurrentBalanceAmount").HasColumnType("decimal(10,2)").IsRequired();
            money.Property(m => m.Currency).HasColumnName("CurrentBalanceCurrency").HasMaxLength(3).IsRequired();
        });
        builder.Navigation(g => g.CurrentBalance).IsRequired();

        builder.Property(g => g.CreatedBy).HasMaxLength(256);
        builder.Property(g => g.ModifiedBy).HasMaxLength(256);

        builder.OwnsMany(g => g.Transactions, transaction =>
        {
            transaction.ToTable("GiftCardTransactions");
            transaction.WithOwner().HasForeignKey("GiftCardId");
            transaction.HasKey(t => t.Id);
            transaction.Property(t => t.Id).ValueGeneratedNever();

            transaction.Property(t => t.Type).HasConversion<string>().HasMaxLength(20);
            transaction.Property(t => t.Notes).HasMaxLength(500);

            transaction.OwnsOne(t => t.Amount, money =>
            {
                money.Property(m => m.Amount).HasColumnName("Amount").HasColumnType("decimal(10,2)").IsRequired();
                money.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
            });

            transaction.Navigation(t => t.Amount).IsRequired();
        });

        builder.HasIndex(g => g.IssuedToMemberId);

        builder.Property(g => g.RowVersion).IsRowVersion();
    }
}

using GymManager.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.Sku).HasMaxLength(50).IsRequired();
        builder.HasIndex(p => p.Sku).IsUnique();

        builder.Property(p => p.Category).HasConversion<string>().HasMaxLength(20);

        builder.Property(p => p.CreatedBy).HasMaxLength(256);
        builder.Property(p => p.ModifiedBy).HasMaxLength(256);
        builder.Property(p => p.DeletedBy).HasMaxLength(256);

        builder.OwnsOne(p => p.Price, price =>
        {
            price.Property(m => m.Amount).HasColumnName("Price").HasColumnType("decimal(18,2)").IsRequired();
            price.Property(m => m.Currency).HasColumnName("Currency").HasMaxLength(3).IsRequired();
        });

        builder.OwnsOne(p => p.CostPrice, price =>
        {
            price.Property(m => m.Amount).HasColumnName("CostPrice").HasColumnType("decimal(18,2)").IsRequired();
            price.Property(m => m.Currency).HasColumnName("CostCurrency").HasMaxLength(3).IsRequired();
        });

        builder.Navigation(p => p.Price).IsRequired();
        builder.Navigation(p => p.CostPrice).IsRequired();

        builder.HasIndex(p => p.BranchId);

        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}

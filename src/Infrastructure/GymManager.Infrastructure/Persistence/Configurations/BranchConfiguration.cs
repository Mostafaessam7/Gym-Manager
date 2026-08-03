using GymManager.Domain.Branches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.Name).HasMaxLength(150).IsRequired();
        builder.HasIndex(b => b.Name).IsUnique();

        builder.Property(b => b.PhoneNumber).HasMaxLength(30);
        builder.Property(b => b.Email).HasMaxLength(256);

        builder.Property(b => b.CreatedBy).HasMaxLength(256);
        builder.Property(b => b.ModifiedBy).HasMaxLength(256);
        builder.Property(b => b.DeletedBy).HasMaxLength(256);

        builder.OwnsOne(b => b.Address, address =>
        {
            address.Property(a => a.Street).HasColumnName("Street").HasMaxLength(200);
            address.Property(a => a.City).HasColumnName("City").HasMaxLength(100);
            address.Property(a => a.State).HasColumnName("State").HasMaxLength(100);
            address.Property(a => a.PostalCode).HasColumnName("PostalCode").HasMaxLength(20);
            address.Property(a => a.Country).HasColumnName("Country").HasMaxLength(100).IsRequired();
        });

        builder.Navigation(b => b.Address).IsRequired();

        builder.Property(b => b.RowVersion).IsRowVersion();
    }
}

using GymManager.Domain.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class GymClassConfiguration : IEntityTypeConfiguration<GymClass>
{
    public void Configure(EntityTypeBuilder<GymClass> builder)
    {
        builder.ToTable("GymClasses");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name).HasMaxLength(150).IsRequired();
        builder.HasIndex(c => c.Name).IsUnique();

        builder.Property(c => c.Description).HasMaxLength(1000);

        builder.Property(c => c.CreatedBy).HasMaxLength(256);
        builder.Property(c => c.ModifiedBy).HasMaxLength(256);
        builder.Property(c => c.DeletedBy).HasMaxLength(256);

        builder.HasIndex(c => c.BranchId);
        builder.HasIndex(c => c.TrainerId);

        builder.Property(c => c.RowVersion).IsRowVersion();
    }
}

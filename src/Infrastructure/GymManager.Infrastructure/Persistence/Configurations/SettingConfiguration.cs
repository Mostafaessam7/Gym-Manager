using GymManager.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.ToTable("Settings");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Key).HasMaxLength(150).IsRequired();
        builder.Property(s => s.Value).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(500);

        builder.Property(s => s.CreatedBy).HasMaxLength(256);
        builder.Property(s => s.ModifiedBy).HasMaxLength(256);

        builder.HasIndex(s => new { s.Key, s.BranchId }).IsUnique();

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}

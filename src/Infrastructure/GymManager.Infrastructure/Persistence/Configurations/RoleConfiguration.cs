using GymManager.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(r => r.Name).IsUnique();

        builder.Property(r => r.Description).HasMaxLength(500);

        builder.Property(r => r.CreatedBy).HasMaxLength(256);
        builder.Property(r => r.ModifiedBy).HasMaxLength(256);

        builder.OwnsMany(r => r.Permissions, permissions =>
        {
            permissions.ToTable("RolePermissions");
            permissions.WithOwner().HasForeignKey("RoleId");
            permissions.Property(p => p.Code).HasMaxLength(100).IsRequired();
            permissions.HasKey("RoleId", nameof(RolePermission.Code));
        });

        builder.Navigation(r => r.Permissions).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(r => r.RowVersion).IsRowVersion();
    }
}

using GymManager.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .HasMaxLength(256)
                .IsRequired();

            email.HasIndex(e => e.Value).IsUnique();
        });

        builder.Navigation(u => u.Email).IsRequired();

        builder.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.PhoneNumber).HasMaxLength(30);
        builder.Property(u => u.FailedLoginAttempts).IsRequired().HasDefaultValue(0);
        builder.Property(u => u.PasswordResetTokenHash).HasMaxLength(128);
        builder.HasIndex(u => u.PasswordResetTokenHash);

        builder.Property(u => u.IsEmailVerified).IsRequired().HasDefaultValue(false);
        builder.Property(u => u.EmailVerificationTokenHash).HasMaxLength(128);
        builder.HasIndex(u => u.EmailVerificationTokenHash);

        builder.Property(u => u.TwoFactorEnabled).IsRequired().HasDefaultValue(false);
        builder.Property(u => u.TwoFactorSecretKey).HasMaxLength(128);
        builder.Property(u => u.TwoFactorChallengeTokenHash).HasMaxLength(128);
        builder.HasIndex(u => u.TwoFactorChallengeTokenHash);

        builder.Property(u => u.CreatedBy).HasMaxLength(256);
        builder.Property(u => u.ModifiedBy).HasMaxLength(256);
        builder.Property(u => u.DeletedBy).HasMaxLength(256);

        builder.OwnsMany(u => u.Roles, roles =>
        {
            roles.ToTable("UserRoles");
            roles.WithOwner().HasForeignKey("UserId");
            roles.HasKey(r => r.Id);
            // The Id is assigned client-side (Guid.NewGuid() in the domain constructor), never by the
            // database. Without this, EF Core's default convention for a Guid key with value-generated-on-add
            // semantics treats an already-non-empty Id as "this must already exist", misclassifying a brand
            // new entry added to an already-tracked collection as Modified instead of Added — which fails
            // with a DbUpdateConcurrencyException since there's no matching row yet to UPDATE.
            roles.Property(r => r.Id).ValueGeneratedNever();
            roles.Property(r => r.RoleId).IsRequired();
            roles.HasIndex("UserId", nameof(UserRole.RoleId)).IsUnique();

            // Shadow (no-navigation) FK from within this owned type's builder — see LeadConfiguration for
            // the rationale, and ClassSessionConfiguration's Bookings for the same pattern applied to an
            // owned collection.
            roles.HasOne<Role>().WithMany().HasForeignKey(r => r.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.OwnsMany(u => u.RefreshTokens, tokens =>
        {
            tokens.ToTable("RefreshTokens");
            tokens.WithOwner().HasForeignKey("UserId");
            tokens.HasKey(t => t.Id);
            tokens.Property(t => t.Id).ValueGeneratedNever();
            tokens.Property(t => t.TokenHash).HasMaxLength(128).IsRequired();
            tokens.Property(t => t.IpAddress).HasMaxLength(64);
            tokens.Property(t => t.UserAgent).HasMaxLength(500);
            tokens.HasIndex(t => t.TokenHash).IsUnique();
        });

        builder.OwnsMany(u => u.PasswordHistory, history =>
        {
            history.ToTable("PasswordHistory");
            history.WithOwner().HasForeignKey("UserId");
            history.HasKey(h => h.Id);
            history.Property(h => h.Id).ValueGeneratedNever();
            history.Property(h => h.PasswordHash).HasMaxLength(500).IsRequired();
        });

        builder.OwnsMany(u => u.TwoFactorRecoveryCodes, codes =>
        {
            codes.ToTable("TwoFactorRecoveryCodes");
            codes.WithOwner().HasForeignKey("UserId");
            codes.HasKey(c => c.Id);
            codes.Property(c => c.Id).ValueGeneratedNever();
            codes.Property(c => c.CodeHash).HasMaxLength(128).IsRequired();
        });

        builder.Navigation(u => u.Roles).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(u => u.RefreshTokens).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(u => u.PasswordHistory).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(u => u.TwoFactorRecoveryCodes).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(u => u.RowVersion).IsRowVersion();
    }
}

using GymManager.Domain.Identity;
using GymManager.Domain.Members;
using GymManager.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.Channel).HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.RecipientAddress).HasMaxLength(256).IsRequired();
        builder.Property(n => n.Subject).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Body).IsRequired();
        builder.Property(n => n.ErrorMessage).HasMaxLength(1000);

        builder.HasIndex(n => n.RecipientUserId);
        builder.HasIndex(n => n.RecipientMemberId);

        // Shadow (no-navigation) FKs — see LeadConfiguration for the rationale. Both nullable: a notification
        // is addressed to either a User or a Member (or neither, e.g. a low-stock digest), never required.
        builder.HasOne<User>().WithMany().HasForeignKey(n => n.RecipientUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Member>().WithMany().HasForeignKey(n => n.RecipientMemberId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(n => n.RowVersion).IsRowVersion();
    }
}

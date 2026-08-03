using GymManager.Domain.Trainers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class TrainerConfiguration : IEntityTypeConfiguration<Trainer>
{
    public void Configure(EntityTypeBuilder<Trainer> builder)
    {
        builder.ToTable("Trainers");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(t => t.LastName).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Specialization).HasMaxLength(150).IsRequired();
        builder.Property(t => t.Bio).HasMaxLength(2000);
        builder.Property(t => t.PhoneNumber).HasMaxLength(30);

        builder.Property(t => t.CreatedBy).HasMaxLength(256);
        builder.Property(t => t.ModifiedBy).HasMaxLength(256);
        builder.Property(t => t.DeletedBy).HasMaxLength(256);

        builder.OwnsOne(t => t.Email, email =>
        {
            email.Property(e => e.Value).HasColumnName("Email").HasMaxLength(256);
            email.HasIndex(e => e.Value).IsUnique().HasFilter("[Email] IS NOT NULL");
        });

        builder.OwnsMany(t => t.Availability, availability =>
        {
            availability.ToTable("TrainerAvailabilitySlots");
            availability.WithOwner().HasForeignKey("TrainerId");
            availability.Property<Guid>("Id").ValueGeneratedOnAdd();
            availability.HasKey("Id");
        });

        builder.Navigation(t => t.Availability).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(t => t.BranchId);

        builder.Property(t => t.RowVersion).IsRowVersion();
    }
}

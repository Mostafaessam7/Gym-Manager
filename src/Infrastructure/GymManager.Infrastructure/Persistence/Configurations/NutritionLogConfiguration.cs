using GymManager.Domain.Members;
using GymManager.Domain.Nutrition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class NutritionLogConfiguration : IEntityTypeConfiguration<NutritionLog>
{
    public void Configure(EntityTypeBuilder<NutritionLog> builder)
    {
        builder.ToTable("NutritionLogs");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.Notes).HasMaxLength(2000);

        builder.Ignore(l => l.TotalCalories);
        builder.Ignore(l => l.TotalProteinG);
        builder.Ignore(l => l.TotalCarbsG);
        builder.Ignore(l => l.TotalFatG);

        builder.OwnsMany(l => l.Entries, entry =>
        {
            entry.ToTable("NutritionLogEntries");
            entry.WithOwner().HasForeignKey("NutritionLogId");
            entry.HasKey(e => e.Id);
            entry.Property(e => e.Id).ValueGeneratedNever();

            entry.Property(e => e.FoodName).HasMaxLength(200).IsRequired();
            entry.Property(e => e.ProteinG).HasColumnType("decimal(6,2)");
            entry.Property(e => e.CarbsG).HasColumnType("decimal(6,2)");
            entry.Property(e => e.FatG).HasColumnType("decimal(6,2)");
            entry.Property(e => e.Notes).HasMaxLength(1000);
        });

        builder.HasIndex(l => new { l.MemberId, l.LoggedOn });

        // Shadow (no-navigation) FK — see LeadConfiguration for the rationale.
        builder.HasOne<Member>().WithMany().HasForeignKey(l => l.MemberId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}

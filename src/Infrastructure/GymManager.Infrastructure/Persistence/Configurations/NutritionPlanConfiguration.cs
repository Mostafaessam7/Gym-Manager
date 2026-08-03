using GymManager.Domain.Nutrition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class NutritionPlanConfiguration : IEntityTypeConfiguration<NutritionPlan>
{
    public void Configure(EntityTypeBuilder<NutritionPlan> builder)
    {
        builder.ToTable("NutritionPlans");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);

        builder.Property(p => p.ProteinTargetG).HasColumnType("decimal(6,2)");
        builder.Property(p => p.CarbsTargetG).HasColumnType("decimal(6,2)");
        builder.Property(p => p.FatTargetG).HasColumnType("decimal(6,2)");

        builder.Property(p => p.CreatedBy).HasMaxLength(256);
        builder.Property(p => p.ModifiedBy).HasMaxLength(256);

        builder.OwnsMany(p => p.Meals, meal =>
        {
            meal.ToTable("NutritionPlanMeals");
            meal.WithOwner().HasForeignKey("NutritionPlanId");
            meal.HasKey(m => m.Id);
            meal.Property(m => m.Id).ValueGeneratedNever();

            meal.Property(m => m.Name).HasMaxLength(200).IsRequired();
            meal.Property(m => m.TimeOfDay).HasMaxLength(50);
            meal.Property(m => m.ProteinG).HasColumnType("decimal(6,2)");
            meal.Property(m => m.CarbsG).HasColumnType("decimal(6,2)");
            meal.Property(m => m.FatG).HasColumnType("decimal(6,2)");
            meal.Property(m => m.Notes).HasMaxLength(1000);
        });

        builder.HasIndex(p => new { p.MemberId, p.IsActive });

        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}

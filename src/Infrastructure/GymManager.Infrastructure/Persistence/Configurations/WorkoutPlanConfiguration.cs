using GymManager.Domain.Members;
using GymManager.Domain.Workouts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class WorkoutPlanConfiguration : IEntityTypeConfiguration<WorkoutPlan>
{
    public void Configure(EntityTypeBuilder<WorkoutPlan> builder)
    {
        builder.ToTable("WorkoutPlans");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);

        builder.Property(p => p.CreatedBy).HasMaxLength(256);
        builder.Property(p => p.ModifiedBy).HasMaxLength(256);

        builder.OwnsMany(p => p.Exercises, exercise =>
        {
            exercise.ToTable("WorkoutPlanExercises");
            exercise.WithOwner().HasForeignKey("WorkoutPlanId");
            exercise.HasKey(e => e.Id);
            exercise.Property(e => e.Id).ValueGeneratedNever();

            exercise.Property(e => e.ExerciseName).HasMaxLength(200).IsRequired();
            exercise.Property(e => e.WeightKg).HasColumnType("decimal(6,2)");
            exercise.Property(e => e.Notes).HasMaxLength(1000);
        });

        builder.HasIndex(p => new { p.MemberId, p.IsActive });

        // Shadow (no-navigation) FK — see LeadConfiguration for the rationale.
        builder.HasOne<Member>().WithMany().HasForeignKey(p => p.MemberId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}

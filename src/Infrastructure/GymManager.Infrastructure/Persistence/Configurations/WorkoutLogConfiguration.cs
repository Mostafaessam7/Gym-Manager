using GymManager.Domain.Members;
using GymManager.Domain.Workouts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymManager.Infrastructure.Persistence.Configurations;

internal sealed class WorkoutLogConfiguration : IEntityTypeConfiguration<WorkoutLog>
{
    public void Configure(EntityTypeBuilder<WorkoutLog> builder)
    {
        builder.ToTable("WorkoutLogs");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.Notes).HasMaxLength(2000);

        builder.OwnsMany(l => l.Exercises, exercise =>
        {
            exercise.ToTable("WorkoutLogExercises");
            exercise.WithOwner().HasForeignKey("WorkoutLogId");
            exercise.HasKey(e => e.Id);
            exercise.Property(e => e.Id).ValueGeneratedNever();

            exercise.Property(e => e.ExerciseName).HasMaxLength(200).IsRequired();
            exercise.Property(e => e.WeightKg).HasColumnType("decimal(6,2)");
            exercise.Property(e => e.Notes).HasMaxLength(1000);
        });

        builder.HasIndex(l => new { l.MemberId, l.CompletedOnUtc });

        // Shadow (no-navigation) FK — see LeadConfiguration for the rationale.
        builder.HasOne<Member>().WithMany().HasForeignKey(l => l.MemberId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}

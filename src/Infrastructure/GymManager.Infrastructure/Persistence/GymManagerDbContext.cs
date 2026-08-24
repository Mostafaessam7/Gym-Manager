using System.Linq.Expressions;
using System.Text.Json;
using GymManager.Application.Abstractions;
using GymManager.Domain.Abstractions;
using GymManager.Domain.Attendance;
using GymManager.Domain.AuditLogs;
using GymManager.Domain.BodyMeasurements;
using GymManager.Domain.Branches;
using GymManager.Domain.Classes;
using GymManager.Domain.Crm;
using GymManager.Domain.Expenses;
using GymManager.Domain.GiftCards;
using GymManager.Domain.Identity;
using GymManager.Domain.Invoices;
using GymManager.Domain.Lockers;
using GymManager.Domain.Members;
using GymManager.Domain.Memberships;
using GymManager.Domain.Notifications;
using GymManager.Domain.Nutrition;
using GymManager.Domain.Payments;
using GymManager.Domain.Products;
using GymManager.Domain.Sales;
using GymManager.Domain.Settings;
using GymManager.Domain.Staff;
using GymManager.Domain.Trainers;
using GymManager.Domain.Workouts;
using GymManager.SharedKernel.Auditing;
using GymManager.SharedKernel.Primitives;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence;

/// <summary>
/// The single write-side EF Core context for the application. Vertical slices reference it directly
/// through repository abstractions rather than splitting persistence per module, since the modules
/// share one SQL Server database in this deployment topology.
/// </summary>
public sealed class GymManagerDbContext(
    DbContextOptions<GymManagerDbContext> options,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IDomainEventDispatcher domainEventDispatcher)
    : DbContext(options), IUnitOfWork, IApplicationReadDb
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Branch> Branches => Set<Branch>();

    public DbSet<Member> Members => Set<Member>();

    public DbSet<MembershipPlan> MembershipPlans => Set<MembershipPlan>();

    public DbSet<Membership> Memberships => Set<Membership>();

    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    public DbSet<BodyMeasurement> BodyMeasurements => Set<BodyMeasurement>();

    public DbSet<Trainer> Trainers => Set<Trainer>();

    public DbSet<GymClass> GymClasses => Set<GymClass>();

    public DbSet<ClassSession> ClassSessions => Set<ClassSession>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Invoice> Invoices => Set<Invoice>();

    public DbSet<Expense> Expenses => Set<Expense>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<Locker> Lockers => Set<Locker>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Setting> Settings => Set<Setting>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<WorkoutPlan> WorkoutPlans => Set<WorkoutPlan>();

    public DbSet<WorkoutLog> WorkoutLogs => Set<WorkoutLog>();

    public DbSet<NutritionPlan> NutritionPlans => Set<NutritionPlan>();

    public DbSet<NutritionLog> NutritionLogs => Set<NutritionLog>();

    public DbSet<Lead> Leads => Set<Lead>();

    public DbSet<GiftCard> GiftCards => Set<GiftCard>();

    public DbSet<StaffShift> StaffShifts => Set<StaffShift>();

    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    public DbSet<Commission> Commissions => Set<Commission>();

    IQueryable<User> IApplicationReadDb.Users => Users.AsNoTracking();

    IQueryable<Role> IApplicationReadDb.Roles => Roles.AsNoTracking();

    IQueryable<Branch> IApplicationReadDb.Branches => Branches.AsNoTracking();

    IQueryable<Member> IApplicationReadDb.Members => Members.AsNoTracking();

    IQueryable<MembershipPlan> IApplicationReadDb.MembershipPlans => MembershipPlans.AsNoTracking();

    IQueryable<Membership> IApplicationReadDb.Memberships => Memberships.AsNoTracking();

    IQueryable<AttendanceRecord> IApplicationReadDb.AttendanceRecords => AttendanceRecords.AsNoTracking();

    IQueryable<BodyMeasurement> IApplicationReadDb.BodyMeasurements => BodyMeasurements.AsNoTracking();

    IQueryable<Trainer> IApplicationReadDb.Trainers => Trainers.AsNoTracking();

    IQueryable<GymClass> IApplicationReadDb.GymClasses => GymClasses.AsNoTracking();

    IQueryable<ClassSession> IApplicationReadDb.ClassSessions => ClassSessions.AsNoTracking();

    IQueryable<Payment> IApplicationReadDb.Payments => Payments.AsNoTracking();

    IQueryable<Invoice> IApplicationReadDb.Invoices => Invoices.AsNoTracking();

    IQueryable<Expense> IApplicationReadDb.Expenses => Expenses.AsNoTracking();

    IQueryable<Product> IApplicationReadDb.Products => Products.AsNoTracking();

    IQueryable<Sale> IApplicationReadDb.Sales => Sales.AsNoTracking();

    IQueryable<Locker> IApplicationReadDb.Lockers => Lockers.AsNoTracking();

    IQueryable<AuditLog> IApplicationReadDb.AuditLogs => AuditLogs.AsNoTracking();

    IQueryable<Setting> IApplicationReadDb.Settings => Settings.AsNoTracking();

    IQueryable<Notification> IApplicationReadDb.Notifications => Notifications.AsNoTracking();

    IQueryable<WorkoutPlan> IApplicationReadDb.WorkoutPlans => WorkoutPlans.AsNoTracking();

    IQueryable<WorkoutLog> IApplicationReadDb.WorkoutLogs => WorkoutLogs.AsNoTracking();

    IQueryable<NutritionPlan> IApplicationReadDb.NutritionPlans => NutritionPlans.AsNoTracking();

    IQueryable<NutritionLog> IApplicationReadDb.NutritionLogs => NutritionLogs.AsNoTracking();

    IQueryable<Lead> IApplicationReadDb.Leads => Leads.AsNoTracking();

    IQueryable<GiftCard> IApplicationReadDb.GiftCards => GiftCards.AsNoTracking();

    IQueryable<StaffShift> IApplicationReadDb.StaffShifts => StaffShifts.AsNoTracking();

    IQueryable<LeaveRequest> IApplicationReadDb.LeaveRequests => LeaveRequests.AsNoTracking();

    IQueryable<Commission> IApplicationReadDb.Commissions => Commissions.AsNoTracking();

    /// <summary>The current caller's branch (<see langword="null"/> for an unscoped, HQ-level caller),
    /// re-evaluated per query against whichever context instance is actually executing it. Backs the
    /// branch-isolation global query filter built by <see cref="BranchIsolationFilterFactory"/> — see that
    /// type's remarks for why this needs to be an instance member rather than a value captured once.</summary>
    internal Guid? CurrentBranchId => currentUserService.BranchId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GymManagerDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned())
                continue;

            var clrType = entityType.ClrType;
            LambdaExpression? filter = null;

            if (typeof(ISoftDeletableEntity).IsAssignableFrom(clrType))
                filter = SoftDeleteFilterFactory.Build(clrType);

            var branchFilter = BranchIsolationFilterFactory.Build(clrType, this);
            if (branchFilter is not null)
                filter = filter is null ? branchFilter : CombineFilters(filter, branchFilter);

            if (filter is not null)
                modelBuilder.Entity(clrType).HasQueryFilter(filter);
        }

        base.OnModelCreating(modelBuilder);
    }

    private static LambdaExpression CombineFilters(LambdaExpression first, LambdaExpression second)
    {
        var parameter = first.Parameters[0];
        var secondBody = new ParameterReplacer(second.Parameters[0], parameter).Visit(second.Body);
        return Expression.Lambda(Expression.AndAlso(first.Body, secondBody), parameter);
    }

    /// <summary>Rewrites a lambda's parameter references so two single-parameter filter expressions built for
    /// the same entity type (but with structurally distinct <see cref="ParameterExpression"/> instances) can be
    /// combined into one, via <see cref="Expression.AndAlso(Expression, Expression)"/>.</summary>
    private sealed class ParameterReplacer(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == source ? target : node;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        RecordAuditLogs();

        var entitiesWithEvents = ChangeTracker.Entries<IHasDomainEvents>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = entitiesWithEvents.SelectMany(e => e.DomainEvents).ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var entity in entitiesWithEvents)
            entity.ClearDomainEvents();

        if (domainEvents.Count > 0)
            await domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);

        return result;
    }

    private void ApplyAuditInformation()
    {
        var now = dateTimeProvider.UtcNow;
        var user = currentUserService.Email ?? "system";

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreated(now, user);
                    break;
                case EntityState.Modified:
                    entry.Entity.SetModified(now, user);
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ISoftDeletableEntity>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.Delete(now, user);
            }
        }
    }

    private void RecordAuditLogs()
    {
        var auditableEntries = ChangeTracker.Entries<IAuditableEntity>()
            .Where(e => !e.Metadata.IsOwned() && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (auditableEntries.Count == 0)
            return;

        var userId = currentUserService.UserId;
        var userEmail = currentUserService.Email;

        foreach (var entry in auditableEntries)
        {
            var entityId = entry.Metadata.FindPrimaryKey()!.Properties
                .Select(p => entry.Property(p.Name).CurrentValue?.ToString())
                .FirstOrDefault() ?? string.Empty;

            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Created,
                EntityState.Deleted => AuditAction.Deleted,
                _ => AuditAction.Updated,
            };

            var changes = entry.Properties
                .Where(p => action == AuditAction.Created ? p.CurrentValue is not null : p.IsModified || action == AuditAction.Deleted)
                .ToDictionary(
                    p => p.Metadata.Name,
                    p => new { Old = action == AuditAction.Created ? null : p.OriginalValue, New = action == AuditAction.Deleted ? null : p.CurrentValue });

            if (changes.Count == 0)
                continue;

            var auditLog = new AuditLog(
                entry.Metadata.ClrType.Name, entityId, action, JsonSerializer.Serialize(changes), userId, userEmail);

            AuditLogs.Add(auditLog);
        }
    }
}

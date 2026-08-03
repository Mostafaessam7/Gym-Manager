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

namespace GymManager.Application.Abstractions;

/// <summary>
/// Read-only, queryable projection of the persistence store used by query handlers that need filtering,
/// sorting or paging beyond what a single-aggregate repository method should expose. Write operations
/// always go through the aggregate repositories instead.
/// </summary>
public interface IApplicationReadDb
{
    IQueryable<User> Users { get; }

    IQueryable<Role> Roles { get; }

    IQueryable<Branch> Branches { get; }

    IQueryable<Member> Members { get; }

    IQueryable<MembershipPlan> MembershipPlans { get; }

    IQueryable<Membership> Memberships { get; }

    IQueryable<AttendanceRecord> AttendanceRecords { get; }

    IQueryable<BodyMeasurement> BodyMeasurements { get; }

    IQueryable<Trainer> Trainers { get; }

    IQueryable<GymClass> GymClasses { get; }

    IQueryable<ClassSession> ClassSessions { get; }

    IQueryable<Payment> Payments { get; }

    IQueryable<Invoice> Invoices { get; }

    IQueryable<Expense> Expenses { get; }

    IQueryable<Product> Products { get; }

    IQueryable<Sale> Sales { get; }

    IQueryable<Locker> Lockers { get; }

    IQueryable<AuditLog> AuditLogs { get; }

    IQueryable<Setting> Settings { get; }

    IQueryable<Notification> Notifications { get; }

    IQueryable<WorkoutPlan> WorkoutPlans { get; }

    IQueryable<WorkoutLog> WorkoutLogs { get; }

    IQueryable<NutritionPlan> NutritionPlans { get; }

    IQueryable<NutritionLog> NutritionLogs { get; }

    IQueryable<Lead> Leads { get; }

    IQueryable<GiftCard> GiftCards { get; }

    IQueryable<StaffShift> StaffShifts { get; }

    IQueryable<LeaveRequest> LeaveRequests { get; }

    IQueryable<Commission> Commissions { get; }
}

namespace GymManager.Domain.Identity;

/// <summary>
/// The full catalog of granular permission codes the system understands. Roles are an assignable bundle
/// of these codes; authorization checks always test a specific permission, never a role name directly.
/// </summary>
public static class Permissions
{
    public static class Users
    {
        public const string View = "users:view";
        public const string Create = "users:create";
        public const string Update = "users:update";
        public const string Deactivate = "users:deactivate";
        public const string ManageRoles = "users:manage-roles";
    }

    public static class Roles
    {
        public const string View = "roles:view";
        public const string Manage = "roles:manage";
    }

    public static class Members
    {
        public const string View = "members:view";
        public const string Create = "members:create";
        public const string Update = "members:update";
        public const string Delete = "members:delete";
    }

    public static class Memberships
    {
        public const string View = "memberships:view";
        public const string Manage = "memberships:manage";
        public const string Renew = "memberships:renew";
    }

    public static class Attendance
    {
        public const string View = "attendance:view";
        public const string CheckIn = "attendance:check-in";
    }

    public static class Trainers
    {
        public const string View = "trainers:view";
        public const string Manage = "trainers:manage";
        public const string ManageSchedule = "trainers:manage-schedule";
    }

    public static class Classes
    {
        public const string View = "classes:view";
        public const string Manage = "classes:manage";
        public const string Book = "classes:book";
    }

    public static class Payments
    {
        public const string View = "payments:view";
        public const string Process = "payments:process";
        public const string Refund = "payments:refund";
    }

    public static class Invoices
    {
        public const string View = "invoices:view";
        public const string Manage = "invoices:manage";
    }

    public static class Expenses
    {
        public const string View = "expenses:view";
        public const string Manage = "expenses:manage";
    }

    public static class Inventory
    {
        public const string View = "inventory:view";
        public const string Manage = "inventory:manage";
    }

    public static class Products
    {
        public const string View = "products:view";
        public const string Manage = "products:manage";
    }

    public static class Pos
    {
        public const string Sell = "pos:sell";
    }

    public static class GiftCards
    {
        public const string View = "gift-cards:view";
        public const string Manage = "gift-cards:manage";
    }

    public static class Staff
    {
        public const string View = "staff:view";
        public const string Manage = "staff:manage";
    }

    public static class Lockers
    {
        public const string View = "lockers:view";
        public const string Manage = "lockers:manage";
    }

    public static class Branches
    {
        public const string View = "branches:view";
        public const string Manage = "branches:manage";
    }

    public static class Notifications
    {
        public const string Manage = "notifications:manage";
    }

    public static class Reports
    {
        public const string View = "reports:view";
        public const string Export = "reports:export";
    }

    public static class Dashboard
    {
        public const string View = "dashboard:view";
    }

    public static class AuditLogs
    {
        public const string View = "audit-logs:view";
    }

    public static class Settings
    {
        public const string Manage = "settings:manage";
    }

    public static class Workouts
    {
        public const string View = "workouts:view";
        public const string Manage = "workouts:manage";
    }

    public static class Nutrition
    {
        public const string View = "nutrition:view";
        public const string Manage = "nutrition:manage";
    }

    public static class Crm
    {
        public const string View = "crm:view";
        public const string Manage = "crm:manage";
    }

    /// <summary>Every permission code defined in the catalog, used to seed system roles and validate assignments.</summary>
    public static IReadOnlyCollection<string> All { get; } = typeof(Permissions)
        .GetNestedTypes()
        .SelectMany(t => t.GetFields())
        .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToArray();
}

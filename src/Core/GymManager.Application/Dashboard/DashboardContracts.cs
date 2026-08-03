namespace GymManager.Application.Dashboard;

public sealed record TopTrainerRow(Guid TrainerId, string TrainerName, int SessionCount, int BookingCount);

public sealed record TopProductRow(Guid ProductId, string ProductName, int QuantitySold, decimal Revenue);

public sealed record RecentPaymentRow(Guid PaymentId, Guid MemberId, decimal Amount, string Currency, string Method, DateTimeOffset CreatedOnUtc);

public sealed record RecentCheckInRow(Guid MemberId, string MemberName, DateTimeOffset CheckInUtc, string Method);

public sealed record RevenueByDayRow(DateOnly Date, decimal Amount);

public sealed record DashboardSummaryResponse(
    decimal TodaysRevenue,
    decimal MonthlyRevenue,
    string Currency,
    int ActiveMembers,
    int ExpiredMemberships,
    int MembersExpiringSoon,
    int AttendanceToday,
    int NewMembersThisMonth,
    IReadOnlyCollection<TopTrainerRow> TopTrainers,
    IReadOnlyCollection<TopProductRow> TopSellingProducts,
    IReadOnlyCollection<string> InventoryAlerts,
    IReadOnlyCollection<RecentPaymentRow> RecentPayments,
    IReadOnlyCollection<RecentCheckInRow> RecentCheckIns,
    IReadOnlyCollection<RevenueByDayRow> RevenueLast30Days);

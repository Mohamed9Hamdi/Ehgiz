namespace Ehgiz.Application.DTOs.Admin;

public record AdminDashboardStatsDto(
    int TotalUsers,
    int ActiveUsers,
    int TotalListings,
    int ActiveListings,
    int TotalBookings,
    int ActiveBookings,
    int DisputedBookings,
    int OpenIssueReports,
    int TotalCategories,
    decimal TotalRevenue,
    decimal PendingEscrow);

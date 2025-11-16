using Microsoft.EntityFrameworkCore;
using VehiclePricePrediction.API.Data;

namespace VehiclePricePrediction.API.Services;

public class StatsService : IStatsService
{
    private readonly ApplicationDbContext _context;

    public StatsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetActiveUsersCountAsync()
    {
        return await _context.Users
            .Where(u => (u.IsBlocked == false) && u.Role != "admin" && u.Role != "superadmin")
            .CountAsync();
    }

    public async Task<AdminStatsDto> GetAdminStatsAsync()
    {
        var vehicleStats = await _context.Vehicles
            .GroupBy(v => 1)
            .Select(g => new
            {
                TotalVehicles = g.Count(),
                PendingVehicles = g.Count(v => v.Status == "pending"),
                ApprovedVehicles = g.Count(v => v.Status == "approved"),
                RejectedVehicles = g.Count(v => v.Status == "rejected")
            })
            .FirstOrDefaultAsync();

        var userStats = await _context.Users
            .Where(u => u.Role == "user")
            .GroupBy(u => 1)
            .Select(g => new
            {
                TotalUsers = g.Count(),
                BlockedUsers = g.Count(u => u.IsBlocked)
            })
            .FirstOrDefaultAsync();

        var adminStats = await _context.Users
            .Where(u => u.Role == "admin" || u.Role == "superadmin")
            .CountAsync();

        var revenueStats = await _context.Subscriptions
            .Where(s => s.Status == "active" || s.Status == "cancelled" || s.Status == "expired")
            .GroupBy(s => 1)
            .Select(g => new
            {
                TotalRevenue = g.Sum(s => s.Price),
                MonthlyRevenue = g.Where(s => s.StartDate >= DateTime.UtcNow.AddMonths(-1)).Sum(s => s.Price)
            })
            .FirstOrDefaultAsync();

        var currentMonthUsers = await _context.Users
            .Where(u => u.Role == "user" && u.CreatedAt >= DateTime.UtcNow.AddMonths(-1))
            .CountAsync();

        var lastMonthUsers = await _context.Users
            .Where(u => u.Role == "user" && 
                       u.CreatedAt >= DateTime.UtcNow.AddMonths(-2) && 
                       u.CreatedAt < DateTime.UtcNow.AddMonths(-1))
            .CountAsync();

        var monthlyGrowth = lastMonthUsers > 0
            ? ((currentMonthUsers - lastMonthUsers) / (double)lastMonthUsers) * 100
            : 0;

        return new AdminStatsDto
        {
            TotalVehicles = vehicleStats?.TotalVehicles ?? 0,
            PendingVehicles = vehicleStats?.PendingVehicles ?? 0,
            ApprovedVehicles = vehicleStats?.ApprovedVehicles ?? 0,
            RejectedVehicles = vehicleStats?.RejectedVehicles ?? 0,
            TotalUsers = userStats?.TotalUsers ?? 0,
            BlockedUsers = userStats?.BlockedUsers ?? 0,
            TotalAdmins = adminStats,
            TotalRevenue = revenueStats?.TotalRevenue ?? 0,
            MonthlyRevenue = revenueStats?.MonthlyRevenue ?? 0,
            MonthlyGrowth = Math.Round(monthlyGrowth * 100) / 100
        };
    }
}


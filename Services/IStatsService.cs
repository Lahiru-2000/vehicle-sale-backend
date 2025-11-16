namespace VehiclePricePrediction.API.Services;

public interface IStatsService
{
    Task<int> GetActiveUsersCountAsync();
    Task<AdminStatsDto> GetAdminStatsAsync();
}

public class AdminStatsDto
{
    public int TotalVehicles { get; set; }
    public int PendingVehicles { get; set; }
    public int ApprovedVehicles { get; set; }
    public int RejectedVehicles { get; set; }
    public int TotalUsers { get; set; }
    public int BlockedUsers { get; set; }
    public int TotalAdmins { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public double MonthlyGrowth { get; set; }
}


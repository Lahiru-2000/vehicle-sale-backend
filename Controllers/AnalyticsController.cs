using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePricePrediction.API.Data;

namespace VehiclePricePrediction.API.Controllers;

[ApiController]
[Route("api/admin/analytics")]
[Authorize(Roles = "admin,superadmin")]
public class AnalyticsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AnalyticsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult> GetAnalytics()
    {
        try
        {
            // Get monthly statistics for the last 12 months
            var monthlyStats = new List<object>();
            for (int i = 0; i < 12; i++)
            {
                var month = DateTime.UtcNow.AddMonths(-i);
                var monthStr = month.ToString("yyyy-MM");
                
                var vehicleCount = await _context.Vehicles
                    .Where(v => v.CreatedAt.Year == month.Year && v.CreatedAt.Month == month.Month)
                    .CountAsync();
                
                var userCount = await _context.Users
                    .Where(u => u.CreatedAt.Year == month.Year && u.CreatedAt.Month == month.Month)
                    .CountAsync();

                monthlyStats.Add(new
                {
                    month = monthStr,
                    vehicleCount,
                    userCount
                });
            }

            // Get vehicle type distribution
            var vehicleTypes = await _context.Vehicles
                .Where(v => v.Status == "approved")
                .GroupBy(v => v.Type)
                .Select(g => new { type = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ToListAsync();

            // Get fuel type distribution
            var fuelTypes = await _context.Vehicles
                .Where(v => v.Status == "approved")
                .GroupBy(v => v.FuelType)
                .Select(g => new { fuelType = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ToListAsync();

            // Get price range distribution
            var priceRanges = await _context.Vehicles
                .Where(v => v.Status == "approved")
                .GroupBy(v => v.Price < 100000 ? "Under Rs. 100k" :
                             v.Price < 250000 ? "Rs. 100k - Rs. 250k" :
                             v.Price < 500000 ? "Rs. 250k - Rs. 500k" :
                             v.Price < 1000000 ? "Rs. 500k - Rs. 1M" : "Over Rs. 1M")
                .Select(g => new { priceRange = g.Key, count = g.Count() })
                .ToListAsync();

            // Get recent activity
            var recentVehicles = await _context.Vehicles
                .Include(v => v.User)
                .OrderByDescending(v => v.CreatedAt)
                .Take(20)
                .Select(v => new
                {
                    type = "vehicle",
                    description = v.Title,
                    date = v.CreatedAt,
                    userName = v.User!.Name
                })
                .ToListAsync();

            var recentUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(20)
                .Select(u => new
                {
                    type = "user",
                    description = "New user registered",
                    date = u.CreatedAt,
                    userName = u.Name
                })
                .ToListAsync();

            var recentActivity = recentVehicles.Cast<object>()
                .Concat(recentUsers.Cast<object>())
                .OrderByDescending(x => ((dynamic)x).date)
                .Take(20)
                .ToList();

            return Ok(new
            {
                monthlyStats,
                vehicleTypes,
                fuelTypes,
                priceRanges,
                recentActivity
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }
}


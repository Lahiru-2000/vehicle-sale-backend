using Microsoft.EntityFrameworkCore;
using VehiclePricePrediction.API.Data;
using VehiclePricePrediction.API.DTOs;
using VehiclePricePrediction.API.Models;

namespace VehiclePricePrediction.API.Services;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;

    public AdminService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        return users.Select(u => new UserDto
        {
            Id = u.Id.ToString(),
            Email = u.Email,
            Name = u.Name,
            Role = u.Role,
            IsBlocked = u.IsBlocked,
            Phone = u.Phone,
            LastLogin = u.LastLogin,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        }).ToList();
    }

    public async Task<bool> ToggleUserBlockAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var userIdGuid))
            throw new ArgumentException("Invalid user ID");

        var user = await _context.Users.FindAsync(userIdGuid);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        user.IsBlocked = !user.IsBlocked;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return user.IsBlocked;
    }

    public async Task<bool> ApproveVehicleAsync(string vehicleId)
    {
        if (!int.TryParse(vehicleId, out var vehicleIdInt))
            throw new ArgumentException("Invalid vehicle ID");

        var vehicle = await _context.Vehicles.FindAsync(vehicleIdInt);
        if (vehicle == null)
        {
            throw new KeyNotFoundException("Vehicle not found");
        }

        vehicle.Status = "approved";
        vehicle.ApprovedAt = DateTime.UtcNow;
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectVehicleAsync(string vehicleId)
    {
        if (!int.TryParse(vehicleId, out var vehicleIdInt))
            throw new ArgumentException("Invalid vehicle ID");

        var vehicle = await _context.Vehicles.FindAsync(vehicleIdInt);
        if (vehicle == null)
        {
            throw new KeyNotFoundException("Vehicle not found");
        }

        vehicle.Status = "rejected";
        vehicle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> BulkApproveVehiclesAsync(List<string> vehicleIds)
    {
        var vehicleIdsInt = vehicleIds
            .Where(id => int.TryParse(id, out _))
            .Select(id => int.Parse(id))
            .ToList();

        var vehicles = await _context.Vehicles
            .Where(v => vehicleIdsInt.Contains(v.Id))
            .ToListAsync();

        foreach (var vehicle in vehicles)
        {
            vehicle.Status = "approved";
            vehicle.ApprovedAt = DateTime.UtcNow;
            vehicle.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> BulkDeleteVehiclesAsync(List<string> vehicleIds)
    {
        var vehicleIdsInt = vehicleIds
            .Where(id => int.TryParse(id, out _))
            .Select(id => int.Parse(id))
            .ToList();

        var vehicles = await _context.Vehicles
            .Where(v => vehicleIdsInt.Contains(v.Id))
            .ToListAsync();

        _context.Vehicles.RemoveRange(vehicles);
        await _context.SaveChangesAsync();
        return true;
    }
}


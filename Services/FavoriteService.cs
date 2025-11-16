using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VehiclePricePrediction.API.Data;
using VehiclePricePrediction.API.DTOs;
using VehiclePricePrediction.API.Models;

namespace VehiclePricePrediction.API.Services;

public class FavoriteService : IFavoriteService
{
    private readonly ApplicationDbContext _context;

    public FavoriteService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<VehicleDto>> GetUserFavoritesAsync(Guid userId)
    {
        var favorites = await _context.Favorites
            .Include(f => f.Vehicle)
                .ThenInclude(v => v!.User)
            .Where(f => f.UserId == userId && f.Vehicle!.Status == "approved")
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => f.Vehicle!)
            .ToListAsync();

        var premiumUsers = await _context.Subscriptions
            .Where(s => favorites.Select(v => v.UserId).Contains(s.UserId) &&
                       s.Status == "active" &&
                       s.EndDate > DateTime.UtcNow)
            .Select(s => s.UserId)
            .ToListAsync();

        return favorites.Select(v => MapToVehicleDto(v, premiumUsers.Contains(v.UserId), userId.ToString())).ToList();
    }

    public async Task<bool> AddFavoriteAsync(Guid userId, int vehicleId)
    {
        // Check if vehicle exists and is approved
        var vehicle = await _context.Vehicles.FindAsync(vehicleId);
        if (vehicle == null || vehicle.Status != "approved")
        {
            throw new KeyNotFoundException("Vehicle not found or not available for favorites");
        }

        // Check if already favorited
        var existing = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.VehicleId == vehicleId);

        if (existing != null)
        {
            throw new InvalidOperationException("Vehicle already in favorites");
        }

        // Add to favorites
        var favorite = new Favorite
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            VehicleId = vehicleId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Favorites.Add(favorite);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RemoveFavoriteAsync(Guid userId, int vehicleId)
    {
        var favorite = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.VehicleId == vehicleId);

        if (favorite == null)
        {
            throw new KeyNotFoundException("Favorite not found");
        }

        _context.Favorites.Remove(favorite);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> IsFavoriteAsync(Guid userId, int vehicleId)
    {
        return await _context.Favorites
            .AnyAsync(f => f.UserId == userId && f.VehicleId == vehicleId);
    }

    private VehicleDto MapToVehicleDto(Vehicle vehicle, bool isPremiumUser, string? currentUserId)
    {
        var images = new List<string>();
        try
        {
            if (!string.IsNullOrEmpty(vehicle.Images))
            {
                images = JsonSerializer.Deserialize<List<string>>(vehicle.Images) ?? new List<string>();
            }
        }
        catch { }

        var contactInfo = new ContactInfo();
        try
        {
            if (!string.IsNullOrEmpty(vehicle.ContactInfo))
            {
                contactInfo = JsonSerializer.Deserialize<ContactInfo>(vehicle.ContactInfo) ?? new ContactInfo();
            }
        }
        catch { }

        var shouldExposeEmail = !string.IsNullOrEmpty(currentUserId) &&
                                (currentUserId == vehicle.UserId.ToString() ||
                                 vehicle.User?.Role == "admin" ||
                                 vehicle.User?.Role == "superadmin");

        return new VehicleDto
        {
            Id = vehicle.Id.ToString(),
            Title = vehicle.Title,
            Brand = vehicle.Brand,
            Model = vehicle.Model,
            Year = vehicle.Year,
            Price = vehicle.Price,
            Type = vehicle.Type,
            FuelType = vehicle.FuelType,
            Transmission = vehicle.Transmission,
            Condition = vehicle.Condition,
            Mileage = vehicle.Mileage,
            Description = vehicle.Description,
            Images = images,
            ContactInfo = contactInfo,
            Status = vehicle.Status,
            UserId = vehicle.UserId.ToString(),
            CreatedAt = vehicle.CreatedAt,
            UpdatedAt = vehicle.UpdatedAt ?? DateTime.UtcNow,
            ApprovedAt = vehicle.ApprovedAt,
            IsPremium = vehicle.IsPremium,
            IsPremiumUser = isPremiumUser,
            User = vehicle.User != null ? new UserInfo
            {
                Id = vehicle.User.Id.ToString(),
                Name = vehicle.User.Name,
                Email = shouldExposeEmail ? vehicle.User.Email : null
            } : null
        };
    }
}


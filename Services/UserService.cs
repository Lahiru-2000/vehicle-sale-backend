using Microsoft.EntityFrameworkCore;
using VehiclePricePrediction.API.Data;
using VehiclePricePrediction.API.DTOs;
using VehiclePricePrediction.API.Models;

namespace VehiclePricePrediction.API.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;

    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserDto?> GetUserProfileAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var userIdGuid))
            return null;

        var user = await _context.Users.FindAsync(userIdGuid);
        if (user == null) return null;

        return new UserDto
        {
            Id = user.Id.ToString(),
            Email = user.Email,
            Name = user.Name,
            Role = user.Role,
            IsBlocked = user.IsBlocked,
            Phone = user.Phone,
            LastLogin = user.LastLogin,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}


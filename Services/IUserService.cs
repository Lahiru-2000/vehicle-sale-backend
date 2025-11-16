using VehiclePricePrediction.API.DTOs;

namespace VehiclePricePrediction.API.Services;

public interface IUserService
{
    Task<UserDto?> GetUserProfileAsync(string userId);
}


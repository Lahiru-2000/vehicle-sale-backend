using VehiclePricePrediction.API.DTOs;

namespace VehiclePricePrediction.API.Services;

public interface IFavoriteService
{
    Task<List<VehicleDto>> GetUserFavoritesAsync(Guid userId);
    Task<bool> AddFavoriteAsync(Guid userId, int vehicleId);
    Task<bool> RemoveFavoriteAsync(Guid userId, int vehicleId);
    Task<bool> IsFavoriteAsync(Guid userId, int vehicleId);
}


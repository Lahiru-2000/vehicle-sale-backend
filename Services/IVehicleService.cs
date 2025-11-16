using VehiclePricePrediction.API.DTOs;

namespace VehiclePricePrediction.API.Services;

public interface IVehicleService
{
    Task<VehicleListResponse> GetVehiclesAsync(VehicleQueryParams queryParams, string? userId = null);
    Task<VehicleTableListResponse> GetVehiclesTableAsync(VehicleQueryParams queryParams, string? userId = null);
    Task<VehicleDto?> GetVehicleByIdAsync(string vehicleId, string? currentUserId = null);
    Task<VehicleDto> CreateVehicleAsync(CreateVehicleRequest request, string userId);
    Task<VehicleDto> UpdateVehicleAsync(string vehicleId, UpdateVehicleRequest request, string userId);
    Task<bool> DeleteVehicleAsync(string vehicleId, string userId);
    Task<PricePredictionResponse> PredictPriceAsync(PricePredictionRequest request, IConfiguration configuration);
}


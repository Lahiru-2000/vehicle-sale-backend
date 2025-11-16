using VehiclePricePrediction.API.DTOs;

namespace VehiclePricePrediction.API.Services;

public interface IAdminService
{
    Task<List<UserDto>> GetAllUsersAsync();
    Task<bool> ToggleUserBlockAsync(string userId);
    Task<bool> ApproveVehicleAsync(string vehicleId);
    Task<bool> RejectVehicleAsync(string vehicleId);
    Task<bool> BulkApproveVehiclesAsync(List<string> vehicleIds);
    Task<bool> BulkDeleteVehiclesAsync(List<string> vehicleIds);
}


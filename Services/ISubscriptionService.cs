using VehiclePricePrediction.API.DTOs;

namespace VehiclePricePrediction.API.Services;

public interface ISubscriptionService
{
    Task<List<SubscriptionPlanDto>> GetSubscriptionPlansAsync();
    Task<SubscriptionStatusResponse> GetUserSubscriptionStatusAsync(string userId);
    Task<CreateSubscriptionResponse> CreateSubscriptionAsync(CreateSubscriptionRequest request, string userId);
    Task<bool> CancelSubscriptionAsync(string userId);
}


using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VehiclePricePrediction.API.Data;
using VehiclePricePrediction.API.DTOs;
using VehiclePricePrediction.API.Models;

namespace VehiclePricePrediction.API.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ApplicationDbContext _context;

    public SubscriptionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SubscriptionPlanDto>> GetSubscriptionPlansAsync()
    {
        var plans = await _context.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.PlanType)
            .ThenBy(p => p.Price)
            .ToListAsync();

        return plans.Select(p => new SubscriptionPlanDto
        {
            Id = p.Id,
            Name = p.Name,
            PlanType = p.PlanType,
            Price = p.Price,
            PostCount = p.PostCount,
            Features = string.IsNullOrEmpty(p.Features)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(p.Features) ?? new List<string>(),
            IsActive = p.IsActive
        }).ToList();
    }

    public async Task<SubscriptionStatusResponse> GetUserSubscriptionStatusAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var userIdGuid))
            throw new ArgumentException("Invalid user ID");

        var subscription = await _context.Subscriptions
            .Where(s => s.UserId == userIdGuid)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (subscription == null)
        {
            return new SubscriptionStatusResponse
            {
                HasActiveSubscription = false,
                Subscription = null
            };
        }

        var plan = await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.PlanType == subscription.PlanType);

        var isActive = subscription.Status == "active" && subscription.EndDate > DateTime.UtcNow;

            return new SubscriptionStatusResponse
            {
                HasActiveSubscription = isActive,
                Subscription = new SubscriptionDto
                {
                    Id = subscription.Id,
                    UserId = subscription.UserId.ToString(),
                    PlanType = subscription.PlanType,
                    Status = subscription.Status,
                    StartDate = subscription.StartDate,
                    EndDate = subscription.EndDate,
                Price = subscription.Price,
                PaymentMethod = subscription.PaymentMethod,
                TransactionId = subscription.TransactionId,
                PostCount = subscription.PostCount,
                CreatedAt = subscription.CreatedAt,
                UpdatedAt = subscription.UpdatedAt,
                CancelledAt = subscription.CancelledAt,
                PlanName = plan?.Name,
                PlanFeatures = plan != null && !string.IsNullOrEmpty(plan.Features)
                    ? JsonSerializer.Deserialize<List<string>>(plan.Features)
                    : new List<string>()
            }
        };
    }

    public async Task<CreateSubscriptionResponse> CreateSubscriptionAsync(CreateSubscriptionRequest request, string userId)
    {
        if (!Guid.TryParse(userId, out var userIdGuid))
            throw new ArgumentException("Invalid user ID");

        SubscriptionPlan? plan = null;

        if (!string.IsNullOrEmpty(request.PlanId))
        {
            plan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Id == request.PlanId && p.IsActive);
        }
        else if (!string.IsNullOrEmpty(request.PlanType))
        {
            plan = await _context.SubscriptionPlans
                .Where(p => p.PlanType == request.PlanType && p.IsActive)
                .OrderBy(p => p.Price)
                .FirstOrDefaultAsync();
        }

        if (plan == null)
        {
            throw new KeyNotFoundException("Subscription plan not found");
        }

        // Check if user already has an active subscription
        var existingSubscription = await _context.Subscriptions
            .Where(s => s.UserId == userIdGuid &&
                       (s.Status == "active" || s.Status == null) &&
                       s.EndDate > DateTime.UtcNow)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (existingSubscription != null)
        {
            throw new InvalidOperationException("User already has an active subscription");
        }

        // Calculate subscription dates (1 month duration)
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddMonths(1);

        // Create subscription
        var subscription = new Subscription
        {
            Id = $"sub-{DateTime.UtcNow.Ticks}-{Guid.NewGuid().ToString("N")[..9]}",
            UserId = userIdGuid,
            PlanType = plan.PlanType,
            Status = "active",
            StartDate = startDate,
            EndDate = endDate,
            Price = plan.Price,
            PaymentMethod = request.PaymentMethod ?? "card",
            TransactionId = request.TransactionId ?? $"txn-{DateTime.UtcNow.Ticks}",
            PostCount = plan.PostCount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        return new CreateSubscriptionResponse
        {
            Message = "Subscription created successfully",
            Subscription = new SubscriptionDto
            {
                Id = subscription.Id,
                UserId = subscription.UserId.ToString(),
                PlanType = subscription.PlanType,
                Status = subscription.Status,
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                Price = subscription.Price,
                PaymentMethod = subscription.PaymentMethod,
                TransactionId = subscription.TransactionId,
                PostCount = subscription.PostCount,
                CreatedAt = subscription.CreatedAt,
                UpdatedAt = subscription.UpdatedAt,
                CancelledAt = subscription.CancelledAt,
                PlanName = plan.Name,
                PlanFeatures = string.IsNullOrEmpty(plan.Features)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(plan.Features) ?? new List<string>()
            }
        };
    }

    public async Task<bool> CancelSubscriptionAsync(string userId)
    {
        if (!Guid.TryParse(userId, out var userIdGuid))
            throw new ArgumentException("Invalid user ID");

        var subscription = await _context.Subscriptions
            .Where(s => s.UserId == userIdGuid &&
                       s.Status == "active" &&
                       s.EndDate > DateTime.UtcNow)
            .FirstOrDefaultAsync();

        if (subscription == null)
        {
            throw new KeyNotFoundException("No active subscription found to cancel");
        }

        subscription.Status = "cancelled";
        subscription.CancelledAt = DateTime.UtcNow;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}


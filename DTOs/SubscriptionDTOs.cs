namespace VehiclePricePrediction.API.DTOs;

public class SubscriptionPlanDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PlanType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int PostCount { get; set; }
    public List<string> Features { get; set; } = new();
    public bool IsActive { get; set; }
}

public class SubscriptionDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string PlanType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Price { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public int PostCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? PlanName { get; set; }
    public List<string>? PlanFeatures { get; set; }
}

public class SubscriptionStatusResponse
{
    public bool HasActiveSubscription { get; set; }
    public SubscriptionDto? Subscription { get; set; }
}

public class CreateSubscriptionRequest
{
    public string? PlanId { get; set; }
    public string? PlanType { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
}

public class CreateSubscriptionResponse
{
    public string Message { get; set; } = string.Empty;
    public SubscriptionDto Subscription { get; set; } = null!;
}

public class CreateSubscriptionPlanRequest
{
    public string Name { get; set; } = string.Empty;
    public string PlanType { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int PostCount { get; set; }
    public List<string>? Features { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdateSubscriptionPlanRequest
{
    public string? Name { get; set; }
    public string? PlanType { get; set; }
    public decimal? Price { get; set; }
    public int? PostCount { get; set; }
    public List<string>? Features { get; set; }
    public bool? IsActive { get; set; }
}


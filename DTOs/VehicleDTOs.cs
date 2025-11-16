namespace VehiclePricePrediction.API.DTOs;

public class VehicleDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public string Type { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public string Transmission { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public int Mileage { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Images { get; set; } = new();
    public ContactInfo ContactInfo { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool IsPremium { get; set; }
    public bool IsPremiumUser { get; set; }
    public UserInfo? User { get; set; }
}

public class ContactInfo
{
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
}

public class CreateVehicleRequest
{
    public string Title { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public string Type { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;
    public string Transmission { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public int Mileage { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<string> Images { get; set; } = new();
    public ContactInfo ContactInfo { get; set; } = new();
}

public class AdminCreateVehicleRequest
{
    public string Title { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public string? Type { get; set; }
    public string? FuelType { get; set; }
    public string? Transmission { get; set; }
    public string? Condition { get; set; }
    public int Mileage { get; set; }
    public string? Description { get; set; }
    public List<string> Images { get; set; } = new();
    public ContactInfo? ContactInfo { get; set; }
    public string? Status { get; set; }
    public string? UserId { get; set; } // Optional: specify owner user ID, otherwise uses admin's ID
}

public class UpdateVehicleRequest
{
    public string? Title { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public decimal? Price { get; set; }
    public string? Type { get; set; }
    public string? FuelType { get; set; }
    public string? Transmission { get; set; }
    public string? Condition { get; set; }
    public int? Mileage { get; set; }
    public string? Description { get; set; }
    public List<string>? Images { get; set; }
    public ContactInfo? ContactInfo { get; set; }
}

public class AdminUpdateVehicleRequest
{
    public string? Title { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public decimal? Price { get; set; }
    public string? Type { get; set; }
    public string? FuelType { get; set; }
    public string? Transmission { get; set; }
    public string? Condition { get; set; }
    public int? Mileage { get; set; }
    public string? Description { get; set; }
    public List<string>? Images { get; set; }
    public ContactInfo? ContactInfo { get; set; }
    public string? Status { get; set; } // Admins can update status
}

public class DeleteVehicleRequest
{
    public string VehicleId { get; set; } = string.Empty;
}

public class VehicleListResponse
{
    public List<VehicleDto> Vehicles { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

public class PaginationInfo
{
    public int Page { get; set; }
    public int Limit { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
}

public class VehicleQueryParams
{
    public string? Search { get; set; }
    public string? Type { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinYear { get; set; }
    public int? MaxYear { get; set; }
    public string? FuelType { get; set; }
    public string? Transmission { get; set; }
    public bool MyPosts { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 20;
}

// Lightweight DTO for table display (admin dashboard)
public class VehicleTableDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Price { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public UserInfo? User { get; set; }
}

public class VehicleTableListResponse
{
    public List<VehicleTableDto> Vehicles { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

public class PricePredictionRequest
{
    public string VehicleId { get; set; } = string.Empty;
    public int YearsAhead { get; set; } = 1;
}

public class PricePredictionResponse
{
    public bool Success { get; set; }
    public PredictionData? Prediction { get; set; }
    public VehicleInfo? Vehicle { get; set; }
    public string? Error { get; set; }
    public string? Details { get; set; }
}

public class PredictionData
{
    public decimal CurrentPrice { get; set; }
    public decimal PredictedPrice { get; set; }
    public decimal PriceDifference { get; set; }
    public double PriceChangePercentage { get; set; }
    public double Confidence { get; set; }
    public int YearsAhead { get; set; }
    public string Currency { get; set; } = "LKR";
    public string Market { get; set; } = "Sri Lankan";
    public List<decimal>? PriceTrend { get; set; }
    public string Timestamp { get; set; } = string.Empty;
}

public class VehicleInfo
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Mileage { get; set; }
    public string FuelType { get; set; } = string.Empty;
    public string Transmission { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}


using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VehiclePricePrediction.API.Data;
using VehiclePricePrediction.API.DTOs;
using VehiclePricePrediction.API.Models;

namespace VehiclePricePrediction.API.Services;

public class VehicleService : IVehicleService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;

    public VehicleService(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<VehicleListResponse> GetVehiclesAsync(VehicleQueryParams queryParams, string? userId = null)
    {
        // Use optimized query with left join instead of Include
        var query = from v in _context.Vehicles
                   join u in _context.Users on v.UserId equals u.Id into userJoin
                   from user in userJoin.DefaultIfEmpty()
                   select new
                   {
                       Vehicle = v,
                       User = user
                   };

        // Status filter
        if (!string.IsNullOrEmpty(queryParams.Status))
        {
            query = query.Where(x => x.Vehicle.Status == queryParams.Status);
        }
        else if (!queryParams.MyPosts)
        {
            query = query.Where(x => x.Vehicle.Status == "approved");
        }

        // My posts filter
        if (queryParams.MyPosts && !string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userIdGuid))
        {
            query = query.Where(x => x.Vehicle.UserId == userIdGuid);
        }

        // Search filter
        if (!string.IsNullOrEmpty(queryParams.Search))
        {
            var searchTerm = $"%{queryParams.Search}%";
            query = query.Where(x =>
                EF.Functions.Like(x.Vehicle.Title, searchTerm) ||
                EF.Functions.Like(x.Vehicle.Brand, searchTerm) ||
                EF.Functions.Like(x.Vehicle.Model, searchTerm) ||
                EF.Functions.Like(x.Vehicle.Description, searchTerm));
        }

        // Type filter
        if (!string.IsNullOrEmpty(queryParams.Type))
        {
            query = query.Where(x => x.Vehicle.Type == queryParams.Type);
        }

        // Price range filters
        if (queryParams.MinPrice.HasValue)
        {
            query = query.Where(x => x.Vehicle.Price >= queryParams.MinPrice.Value);
        }
        if (queryParams.MaxPrice.HasValue)
        {
            query = query.Where(x => x.Vehicle.Price <= queryParams.MaxPrice.Value);
        }

        // Year range filters
        if (queryParams.MinYear.HasValue)
        {
            query = query.Where(x => x.Vehicle.Year >= queryParams.MinYear.Value);
        }
        if (queryParams.MaxYear.HasValue)
        {
            query = query.Where(x => x.Vehicle.Year <= queryParams.MaxYear.Value);
        }

        // Fuel type filter
        if (!string.IsNullOrEmpty(queryParams.FuelType))
        {
            query = query.Where(x => x.Vehicle.FuelType == queryParams.FuelType);
        }

        // Transmission filter
        if (!string.IsNullOrEmpty(queryParams.Transmission))
        {
            query = query.Where(x => x.Vehicle.Transmission == queryParams.Transmission);
        }

        // Get total count (before pagination)
        var total = await query.CountAsync();

        // Get user IDs for premium check (from paginated results)
        var paginatedQuery = query
            .OrderByDescending(x => x.Vehicle.CreatedAt)
            .ThenByDescending(x => x.Vehicle.IsPremium)
            .ThenByDescending(x => x.Vehicle.ApprovedAt ?? DateTime.MinValue)
            .Skip((queryParams.Page - 1) * queryParams.Limit)
            .Take(queryParams.Limit);

        // Get vehicles with user data
        var results = await paginatedQuery
            .AsNoTracking()
            .ToListAsync();

        // Check premium user status (only for returned vehicles)
        var userIds = results.Select(x => x.Vehicle.UserId).Distinct().ToList();
        var premiumUsers = userIds.Any() ? await _context.Subscriptions
            .Where(s => userIds.Contains(s.UserId) &&
                       s.Status == "active" &&
                       s.EndDate > DateTime.UtcNow)
            .Select(s => s.UserId)
            .ToListAsync() : new List<Guid>();

        // Map to DTOs efficiently
        var vehicleDtos = results.Select(x =>
        {
            var v = x.Vehicle;
            var isPremiumUser = premiumUsers.Contains(v.UserId);
            
            // Handle null safety for Images
            var images = new List<string>();
            if (!string.IsNullOrEmpty(v.Images))
            {
                try
                {
                    images = JsonSerializer.Deserialize<List<string>>(v.Images) ?? new List<string>();
                }
                catch { }
            }

            // Handle null safety for ContactInfo
            var contactInfo = new ContactInfo();
            if (!string.IsNullOrEmpty(v.ContactInfo))
            {
                try
                {
                    contactInfo = JsonSerializer.Deserialize<ContactInfo>(v.ContactInfo) ?? new ContactInfo();
                }
                catch { }
            }

            // Determine if email should be exposed
            var shouldExposeEmail = !string.IsNullOrEmpty(userId) &&
                                    (userId == v.UserId.ToString() ||
                                     x.User?.Role == "admin" ||
                                     x.User?.Role == "superadmin");

            return new VehicleDto
            {
                Id = v.Id.ToString(),
                Title = v.Title ?? string.Empty,
                Brand = v.Brand ?? string.Empty,
                Model = v.Model ?? string.Empty,
                Year = v.Year,
                Price = v.Price,
                Type = v.Type ?? "car",
                FuelType = v.FuelType ?? "petrol",
                Transmission = v.Transmission ?? "manual",
                Condition = v.Condition ?? "USED",
                Mileage = v.Mileage,
                Description = v.Description ?? string.Empty,
                Images = images,
                ContactInfo = contactInfo,
                Status = v.Status ?? "pending",
                UserId = v.UserId.ToString(),
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt ?? DateTime.UtcNow,
                ApprovedAt = v.ApprovedAt,
                IsPremium = v.IsPremium,
                IsPremiumUser = isPremiumUser,
                User = x.User != null ? new UserInfo
                {
                    Id = x.User.Id.ToString(),
                    Name = x.User.Name ?? string.Empty,
                    Email = shouldExposeEmail ? x.User.Email : null
                } : null
            };
        }).ToList();

        return new VehicleListResponse
        {
            Vehicles = vehicleDtos,
            Pagination = new PaginationInfo
            {
                Page = queryParams.Page,
                Limit = queryParams.Limit,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)queryParams.Limit)
            }
        };
    }

    public async Task<VehicleTableListResponse> GetVehiclesTableAsync(VehicleQueryParams queryParams, string? userId = null)
    {
        // Optimized query - only select fields needed for table display
        var query = from v in _context.Vehicles
                   join u in _context.Users on v.UserId equals u.Id into userJoin
                   from user in userJoin.DefaultIfEmpty()
                   select new
                   {
                       Vehicle = v,
                       User = user
                   };

        // Status filter
        if (!string.IsNullOrEmpty(queryParams.Status))
        {
            query = query.Where(x => x.Vehicle.Status == queryParams.Status);
        }
        else if (!queryParams.MyPosts)
        {
            query = query.Where(x => x.Vehicle.Status == "approved");
        }

        // My posts filter
        if (queryParams.MyPosts && !string.IsNullOrEmpty(userId) && Guid.TryParse(userId, out var userIdGuid))
        {
            query = query.Where(x => x.Vehicle.UserId == userIdGuid);
        }

        // Search filter
        if (!string.IsNullOrEmpty(queryParams.Search))
        {
            var searchTerm = $"%{queryParams.Search}%";
            query = query.Where(x =>
                EF.Functions.Like(x.Vehicle.Title, searchTerm) ||
                EF.Functions.Like(x.Vehicle.Brand, searchTerm) ||
                EF.Functions.Like(x.Vehicle.Model, searchTerm) ||
                EF.Functions.Like(x.Vehicle.Description, searchTerm));
        }

        // Type filter
        if (!string.IsNullOrEmpty(queryParams.Type))
        {
            query = query.Where(x => x.Vehicle.Type == queryParams.Type);
        }

        // Price range filters
        if (queryParams.MinPrice.HasValue)
        {
            query = query.Where(x => x.Vehicle.Price >= queryParams.MinPrice.Value);
        }
        if (queryParams.MaxPrice.HasValue)
        {
            query = query.Where(x => x.Vehicle.Price <= queryParams.MaxPrice.Value);
        }

        // Year range filters
        if (queryParams.MinYear.HasValue)
        {
            query = query.Where(x => x.Vehicle.Year >= queryParams.MinYear.Value);
        }
        if (queryParams.MaxYear.HasValue)
        {
            query = query.Where(x => x.Vehicle.Year <= queryParams.MaxYear.Value);
        }

        // Fuel type filter
        if (!string.IsNullOrEmpty(queryParams.FuelType))
        {
            query = query.Where(x => x.Vehicle.FuelType == queryParams.FuelType);
        }

        // Transmission filter
        if (!string.IsNullOrEmpty(queryParams.Transmission))
        {
            query = query.Where(x => x.Vehicle.Transmission == queryParams.Transmission);
        }

        // Get total count (before pagination)
        var total = await query.CountAsync();

        // Apply pagination and ordering
        var paginatedQuery = query
            .OrderByDescending(x => x.Vehicle.CreatedAt)
            .ThenByDescending(x => x.Vehicle.IsPremium)
            .ThenByDescending(x => x.Vehicle.ApprovedAt ?? DateTime.MinValue)
            .Skip((queryParams.Page - 1) * queryParams.Limit)
            .Take(queryParams.Limit);

        // Get results - only select necessary fields (no Images, ContactInfo, Description)
        var results = await paginatedQuery
            .AsNoTracking()
            .Select(x => new VehicleTableDto
            {
                Id = x.Vehicle.Id.ToString(),
                Title = x.Vehicle.Title ?? string.Empty,
                Brand = x.Vehicle.Brand ?? string.Empty,
                Model = x.Vehicle.Model ?? string.Empty,
                Year = x.Vehicle.Year,
                Price = x.Vehicle.Price,
                Type = x.Vehicle.Type ?? "car",
                Status = x.Vehicle.Status ?? "pending",
                CreatedAt = x.Vehicle.CreatedAt,
                User = x.User != null ? new UserInfo
                {
                    Id = x.User.Id.ToString(),
                    Name = x.User.Name ?? string.Empty,
                    Email = x.User.Email ?? null // Admins can see emails
                } : null
            })
            .ToListAsync();

        return new VehicleTableListResponse
        {
            Vehicles = results,
            Pagination = new PaginationInfo
            {
                Page = queryParams.Page,
                Limit = queryParams.Limit,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)queryParams.Limit)
            }
        };
    }

    public async Task<VehicleDto?> GetVehicleByIdAsync(string vehicleId, string? currentUserId = null)
    {
        if (!int.TryParse(vehicleId, out var vehicleIdInt))
            return null;

        var vehicle = await _context.Vehicles
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.Id == vehicleIdInt);

        if (vehicle == null) return null;

        var isPremiumUser = await _context.Subscriptions
            .AnyAsync(s => s.UserId == vehicle.UserId &&
                          s.Status == "active" &&
                          s.EndDate > DateTime.UtcNow);

        return MapToVehicleDto(vehicle, isPremiumUser, currentUserId);
    }

    public async Task<VehicleDto> CreateVehicleAsync(CreateVehicleRequest request, string userId)
    {
        // Validate
        ValidateVehicleRequest(request);

        if (!Guid.TryParse(userId, out var userIdGuid))
            throw new ArgumentException("Invalid user ID");

        // Check subscription for premium status
        var subscription = await _context.Subscriptions
            .Where(s => s.UserId == userIdGuid &&
                       (s.Status == "active" || s.Status == null) &&
                       s.EndDate > DateTime.UtcNow)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        var isPremium = subscription != null && subscription.PostCount > 0;

        // Create vehicle
        var vehicle = new Vehicle
        {
            Title = request.Title,
            Brand = request.Brand,
            Model = request.Model,
            Year = request.Year,
            Price = request.Price,
            Type = request.Type,
            FuelType = request.FuelType,
            Transmission = request.Transmission,
            Condition = request.Condition,
            Mileage = request.Mileage,
            Description = request.Description,
            Images = JsonSerializer.Serialize(request.Images),
            ContactInfo = JsonSerializer.Serialize(request.ContactInfo),
            Status = "pending",
            UserId = userIdGuid,
            IsPremium = isPremium,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Vehicles.Add(vehicle);

        // Decrement post count if subscription exists
        if (subscription != null && subscription.PostCount > 0)
        {
            subscription.PostCount--;
            subscription.UpdatedAt = DateTime.UtcNow;

            // Auto-cancel if no posts remaining
            if (subscription.PostCount <= 0)
            {
                subscription.Status = "cancelled";
                subscription.CancelledAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        var isPremiumUser = subscription != null && subscription.Status == "active" && subscription.EndDate > DateTime.UtcNow;
        return MapToVehicleDto(vehicle, isPremiumUser, userId);
    }

    public async Task<VehicleDto> UpdateVehicleAsync(string vehicleId, UpdateVehicleRequest request, string userId)
    {
        if (!int.TryParse(vehicleId, out var vehicleIdInt))
            throw new ArgumentException("Invalid vehicle ID");
        if (!Guid.TryParse(userId, out var userIdGuid))
            throw new ArgumentException("Invalid user ID");

        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == vehicleIdInt && v.UserId == userIdGuid);

        if (vehicle == null)
        {
            throw new KeyNotFoundException("Vehicle not found or you do not have permission to edit it");
        }

        if (vehicle.Status != "pending")
        {
            throw new InvalidOperationException("Only pending vehicles can be edited");
        }

        // Update fields
        if (!string.IsNullOrEmpty(request.Title)) vehicle.Title = request.Title;
        if (!string.IsNullOrEmpty(request.Brand)) vehicle.Brand = request.Brand;
        if (!string.IsNullOrEmpty(request.Model)) vehicle.Model = request.Model;
        if (request.Year.HasValue) vehicle.Year = request.Year.Value;
        if (request.Price.HasValue) vehicle.Price = request.Price.Value;
        if (!string.IsNullOrEmpty(request.Type)) vehicle.Type = request.Type;
        if (!string.IsNullOrEmpty(request.FuelType)) vehicle.FuelType = request.FuelType;
        if (!string.IsNullOrEmpty(request.Transmission)) vehicle.Transmission = request.Transmission;
        if (!string.IsNullOrEmpty(request.Condition)) vehicle.Condition = request.Condition;
        if (request.Mileage.HasValue) vehicle.Mileage = request.Mileage.Value;
        if (!string.IsNullOrEmpty(request.Description)) vehicle.Description = request.Description;
        if (request.Images != null) vehicle.Images = JsonSerializer.Serialize(request.Images);
        if (request.ContactInfo != null) vehicle.ContactInfo = JsonSerializer.Serialize(request.ContactInfo);

        vehicle.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var isPremiumUser = await _context.Subscriptions
            .AnyAsync(s => s.UserId == userIdGuid &&
                          s.Status == "active" &&
                          s.EndDate > DateTime.UtcNow);

        return MapToVehicleDto(vehicle, isPremiumUser, userId);
    }

    public async Task<bool> DeleteVehicleAsync(string vehicleId, string userId)
    {
        if (!int.TryParse(vehicleId, out var vehicleIdInt))
            throw new ArgumentException("Invalid vehicle ID");
        if (!Guid.TryParse(userId, out var userIdGuid))
            throw new ArgumentException("Invalid user ID");

        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.Id == vehicleIdInt && v.UserId == userIdGuid);

        if (vehicle == null)
        {
            throw new KeyNotFoundException("Vehicle not found or you do not have permission to delete it");
        }

        if (vehicle.Status != "pending")
        {
            throw new InvalidOperationException("Only pending vehicles can be deleted");
        }

        _context.Vehicles.Remove(vehicle);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<PricePredictionResponse> PredictPriceAsync(PricePredictionRequest request, IConfiguration configuration)
    {
        if (string.IsNullOrEmpty(request.VehicleId))
        {
            return new PricePredictionResponse
            {
                Success = false,
                Error = "Vehicle ID is required"
            };
        }

        if (request.YearsAhead < 0 || request.YearsAhead > 5)
        {
            return new PricePredictionResponse
            {
                Success = false,
                Error = "Years ahead must be between 0 and 5"
            };
        }

        var vehicle = await GetVehicleByIdAsync(request.VehicleId);
        if (vehicle == null)
        {
            return new PricePredictionResponse
            {
                Success = false,
                Error = "Vehicle not found"
            };
        }

        if (vehicle.Type != "car")
        {
            return new PricePredictionResponse
            {
                Success = false,
                Error = "Price prediction is only available for cars"
            };
        }

        // Call ML API
        var mlApiUrl = configuration["MLApi:BaseUrl"] ?? "http://localhost:5000";
        var httpClient = _httpClientFactory.CreateClient();

        var mlRequest = new
        {
            brand = vehicle.Brand.ToUpper(),
            model = vehicle.Model,
            year = vehicle.Year,
            mileage = vehicle.Mileage,
            current_price = vehicle.Price,
            condition = vehicle.Condition,
            years_ahead = request.YearsAhead
        };

        try
        {
            var response = await httpClient.PostAsJsonAsync($"{mlApiUrl}/predict", mlRequest);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new PricePredictionResponse
                {
                    Success = false,
                    Error = "Failed to get price prediction",
                    Details = errorContent
                };
            }

            var predictionData = await response.Content.ReadFromJsonAsync<MLPredictionResponse>();

            if (predictionData == null)
            {
                return new PricePredictionResponse
                {
                    Success = false,
                    Error = "Invalid response from ML service"
                };
            }

            var priceDifference = predictionData.predicted_price - vehicle.Price;
            var priceChangePercentage = ((double)priceDifference / (double)vehicle.Price) * 100;

            return new PricePredictionResponse
            {
                Success = true,
                Prediction = new PredictionData
                {
                    CurrentPrice = vehicle.Price,
                    PredictedPrice = predictionData.predicted_price,
                    PriceDifference = priceDifference,
                    PriceChangePercentage = priceChangePercentage,
                    Confidence = predictionData.confidence,
                    YearsAhead = predictionData.years_ahead,
                    Currency = predictionData.currency ?? "LKR",
                    Market = predictionData.market ?? "Sri Lankan",
                    PriceTrend = predictionData.price_trend,
                    Timestamp = predictionData.timestamp ?? DateTime.UtcNow.ToString("O")
                },
                Vehicle = new VehicleInfo
                {
                    Id = vehicle.Id,
                    Title = vehicle.Title,
                    Brand = vehicle.Brand,
                    Model = vehicle.Model,
                    Year = vehicle.Year,
                    Mileage = vehicle.Mileage,
                    FuelType = vehicle.FuelType,
                    Transmission = vehicle.Transmission,
                    Type = vehicle.Type
                }
            };
        }
        catch (Exception ex)
        {
            return new PricePredictionResponse
            {
                Success = false,
                Error = "Internal server error",
                Details = ex.Message
            };
        }
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
                // Use JsonSerializerOptions to handle case-insensitive property matching
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };
                
                var deserialized = JsonSerializer.Deserialize<ContactInfo>(vehicle.ContactInfo, options);
                if (deserialized != null)
                {
                    contactInfo = deserialized;
                }
                else
                {
                    // If deserialization returns null, log the issue
                    Console.WriteLine($"Warning: ContactInfo deserialization returned null for vehicle {vehicle.Id}. Raw value: {vehicle.ContactInfo}");
                }
            }
            else
            {
                Console.WriteLine($"Warning: ContactInfo is empty or null for vehicle {vehicle.Id}");
            }
        }
        catch (JsonException jsonEx)
        {
            // Log JSON parsing errors specifically
            Console.WriteLine($"JSON parsing error for ContactInfo (vehicle {vehicle.Id}): {jsonEx.Message}");
            Console.WriteLine($"Raw ContactInfo value: {vehicle.ContactInfo ?? "null"}");
            Console.WriteLine($"JSON error path: {jsonEx.Path}, line: {jsonEx.LineNumber}");
        }
        catch (Exception ex)
        {
            // Log other errors
            Console.WriteLine($"Error deserializing ContactInfo for vehicle {vehicle.Id}: {ex.Message}");
            Console.WriteLine($"Raw ContactInfo value: {vehicle.ContactInfo ?? "null"}");
        }

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

    private void ValidateVehicleRequest(CreateVehicleRequest request)
    {
        var requiredFields = new[]
        {
            (request.Title, "title"),
            (request.Brand, "brand"),
            (request.Model, "model"),
            (request.Description, "description")
        };

        foreach (var (value, field) in requiredFields)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{field} is required");
            }
        }

        var currentYear = DateTime.Now.Year;
        if (request.Year < 1900 || request.Year > currentYear + 1)
        {
            throw new ArgumentException("Invalid year");
        }

        if (request.Price <= 0)
        {
            throw new ArgumentException("Price must be greater than 0");
        }

        if (request.Mileage < 0)
        {
            throw new ArgumentException("Mileage cannot be negative");
        }

        var validConditions = new[] { "USED", "BRANDNEW", "REFURBISHED" };
        if (!validConditions.Contains(request.Condition))
        {
            throw new ArgumentException("Invalid condition. Must be USED, BRANDNEW, or REFURBISHED");
        }

        if (request.ContactInfo == null ||
            string.IsNullOrWhiteSpace(request.ContactInfo.Phone) ||
            string.IsNullOrWhiteSpace(request.ContactInfo.Email) ||
            string.IsNullOrWhiteSpace(request.ContactInfo.Location))
        {
            throw new ArgumentException("Complete contact information is required");
        }
    }

    private class MLPredictionResponse
    {
        public decimal predicted_price { get; set; }
        public double confidence { get; set; }
        public int years_ahead { get; set; }
        public string? currency { get; set; }
        public string? market { get; set; }
        public List<decimal>? price_trend { get; set; }
        public string? timestamp { get; set; }
    }
}


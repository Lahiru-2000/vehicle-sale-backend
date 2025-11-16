using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VehiclePricePrediction.API.DTOs;
using VehiclePricePrediction.API.Services;
using AdminStatsDto = VehiclePricePrediction.API.Services.AdminStatsDto;
using Microsoft.EntityFrameworkCore;
using VehiclePricePrediction.API.Data;
using VehiclePricePrediction.API.Models;

namespace VehiclePricePrediction.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin,superadmin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<UserDto>>> GetAllUsers()
    {
        var users = await _adminService.GetAllUsersAsync();
        return Ok(new { users });
    }

    [HttpGet("users/{userId}")]
    public async Task<ActionResult<UserDto>> GetUserById(string userId)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userIdGuid))
            {
                return BadRequest(new { error = "Invalid user ID" });
            }

            var dbContext = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var user = await dbContext.Users.FindAsync(userIdGuid);

            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            var userDto = new UserDto
            {
                Id = user.Id.ToString(),
                Email = user.Email ?? string.Empty,
                Name = user.Name ?? string.Empty,
                Role = user.Role ?? "user",
                IsBlocked = user.IsBlocked,
                Phone = user.Phone,
                LastLogin = user.LastLogin,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return Ok(new { user = userDto });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Internal server error", 
                details = ex.Message
            });
        }
    }

    [HttpGet("admins")]
    public async Task<ActionResult> GetAllAdmins([FromServices] ApplicationDbContext dbContext)
    {
        try
        {
            var admins = await dbContext.Users
                .Where(u => u.Role == "admin" || u.Role == "superadmin")
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var adminDtos = admins.Select(u => new UserDto
            {
                Id = u.Id.ToString(),
                Email = u.Email ?? string.Empty,
                Name = u.Name ?? string.Empty,
                Role = u.Role ?? "user",
                IsBlocked = u.IsBlocked,
                Phone = u.Phone,
                LastLogin = u.LastLogin,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            }).ToList();

            return Ok(new { admins = adminDtos });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Internal server error", 
                details = ex.Message,
                innerException = ex.InnerException?.Message
            });
        }
    }

    [HttpPost("admins/add")]
    [Authorize(Roles = "superadmin")]
    public async Task<ActionResult> CreateAdmin(
        [FromServices] ApplicationDbContext dbContext,
        [FromBody] CreateAdminRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "Name is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { error = "Email is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "Password is required" });
            }

            if (request.Password.Length < 8)
            {
                return BadRequest(new { error = "Password must be at least 8 characters long" });
            }

            if (!request.Email.Contains('@'))
            {
                return BadRequest(new { error = "Please enter a valid email address" });
            }

            // Validate role - only allow admin or superadmin
            var role = string.IsNullOrWhiteSpace(request.Role) ? "admin" : request.Role.ToLower();
            if (role != "admin" && role != "superadmin")
            {
                return BadRequest(new { error = "Invalid role. Must be 'admin' or 'superadmin'" });
            }

            // Check if user already exists
            var existingUser = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);
            
            if (existingUser != null)
            {
                return Conflict(new { error = "User with this email already exists" });
            }

            // Hash password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create admin user
            var admin = new User
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Email = request.Email.Trim().ToLower(),
                Password = hashedPassword,
                Phone = request.Phone?.Trim(),
                Role = role,
                IsBlocked = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.Users.Add(admin);
            await dbContext.SaveChangesAsync();

            // Return admin DTO
            var adminDto = new UserDto
            {
                Id = admin.Id.ToString(),
                Email = admin.Email,
                Name = admin.Name,
                Role = admin.Role,
                IsBlocked = admin.IsBlocked,
                Phone = admin.Phone,
                LastLogin = admin.LastLogin,
                CreatedAt = admin.CreatedAt,
                UpdatedAt = admin.UpdatedAt
            };

            return Ok(new { 
                message = "Admin created successfully",
                admin = adminDto
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Internal server error", 
                details = ex.Message,
                innerException = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    [HttpPost("admins/toggle-block")]
    public async Task<ActionResult> ToggleAdminBlock(
        [FromServices] ApplicationDbContext dbContext,
        [FromBody] ToggleAdminBlockRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.AdminId))
            {
                return BadRequest(new { error = "Admin ID is required" });
            }

            if (!Guid.TryParse(request.AdminId, out var adminIdGuid))
            {
                return BadRequest(new { error = "Invalid admin ID" });
            }

            var admin = await dbContext.Users.FindAsync(adminIdGuid);
            if (admin == null)
            {
                return NotFound(new { error = "Admin not found" });
            }

            // Verify it's actually an admin
            if (admin.Role != "admin" && admin.Role != "superadmin")
            {
                return BadRequest(new { error = "User is not an admin" });
            }

            // Prevent blocking yourself
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == admin.Id.ToString())
            {
                return BadRequest(new { error = "You cannot block yourself" });
            }

            admin.IsBlocked = request.Block;
            admin.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

            return Ok(new { 
                message = $"Admin {(request.Block ? "blocked" : "unblocked")} successfully",
                isBlocked = admin.IsBlocked
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Internal server error", 
                details = ex.Message,
                innerException = ex.InnerException?.Message
            });
        }
    }

    [HttpDelete("admins/delete")]
    public async Task<ActionResult> DeleteAdmin(
        [FromServices] ApplicationDbContext dbContext,
        [FromBody] DeleteAdminRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.AdminId))
            {
                return BadRequest(new { error = "Admin ID is required" });
            }

            if (!Guid.TryParse(request.AdminId, out var adminIdGuid))
            {
                return BadRequest(new { error = "Invalid admin ID" });
            }

            var admin = await dbContext.Users.FindAsync(adminIdGuid);
            if (admin == null)
            {
                return NotFound(new { error = "Admin not found" });
            }

            // Verify it's actually an admin
            if (admin.Role != "admin" && admin.Role != "superadmin")
            {
                return BadRequest(new { error = "User is not an admin" });
            }

            // Prevent deleting yourself
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == admin.Id.ToString())
            {
                return BadRequest(new { error = "You cannot delete yourself" });
            }

            // Prevent deleting superadmin (only allow deleting regular admins)
            if (admin.Role == "superadmin")
            {
                // Check if current user is also superadmin
                var currentUser = await dbContext.Users.FindAsync(Guid.Parse(currentUserId));
                if (currentUser == null || currentUser.Role != "superadmin")
                {
                    return Forbid("Only super admins can delete other super admins");
                }
            }

            // Delete related records first
            var vehicles = await dbContext.Vehicles
                .Where(v => v.UserId == adminIdGuid)
                .ToListAsync();
            
            if (vehicles.Any())
            {
                // Delete favorites for these vehicles
                var vehicleIds = vehicles.Select(v => v.Id).ToList();
                var favorites = await dbContext.Favorites
                    .Where(f => vehicleIds.Contains(f.VehicleId))
                    .ToListAsync();
                if (favorites.Any())
                {
                    dbContext.Favorites.RemoveRange(favorites);
                }

                // Delete vehicle images
                var vehicleImages = await dbContext.VehicleImages
                    .Where(vi => vehicleIds.Contains(vi.VehicleId))
                    .ToListAsync();
                if (vehicleImages.Any())
                {
                    dbContext.VehicleImages.RemoveRange(vehicleImages);
                }

                // Delete vehicles
                dbContext.Vehicles.RemoveRange(vehicles);
            }

            // Delete user's favorites
            var userFavorites = await dbContext.Favorites
                .Where(f => f.UserId == adminIdGuid)
                .ToListAsync();
            if (userFavorites.Any())
            {
                dbContext.Favorites.RemoveRange(userFavorites);
            }

            // Delete user's subscriptions
            var subscriptions = await dbContext.Subscriptions
                .Where(s => s.UserId == adminIdGuid)
                .ToListAsync();
            if (subscriptions.Any())
            {
                dbContext.Subscriptions.RemoveRange(subscriptions);
            }

            // Delete the admin user
            dbContext.Users.Remove(admin);
            await dbContext.SaveChangesAsync();

            return Ok(new { message = "Admin deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Internal server error", 
                details = ex.Message,
                innerException = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    [HttpPost("users/add")]
    public async Task<ActionResult> CreateUser(
        [FromServices] ApplicationDbContext dbContext,
        [FromBody] CreateUserRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "Name is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { error = "Email is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "Password is required" });
            }

            if (request.Password.Length < 6)
            {
                return BadRequest(new { error = "Password must be at least 6 characters long" });
            }

            if (!request.Email.Contains('@'))
            {
                return BadRequest(new { error = "Please enter a valid email address" });
            }

            // Validate role
            var validRoles = new[] { "user", "admin", "superadmin" };
            var role = string.IsNullOrWhiteSpace(request.Role) ? "user" : request.Role.ToLower();
            if (!validRoles.Contains(role))
            {
                return BadRequest(new { error = "Invalid role. Must be 'user', 'admin', or 'superadmin'" });
            }

            // Check if user already exists
            var existingUser = await dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);
            
            if (existingUser != null)
            {
                return Conflict(new { error = "User with this email already exists" });
            }

            // Hash password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                Email = request.Email.Trim().ToLower(),
                Password = hashedPassword,
                Phone = request.Phone?.Trim(),
                Role = role,
                IsBlocked = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            // Return user DTO
            var userDto = new UserDto
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

            return Ok(new { 
                message = "User created successfully",
                user = userDto
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Internal server error", 
                details = ex.Message,
                innerException = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    [HttpPost("vehicles/add")]
    public async Task<ActionResult> AddVehicle(
        [FromServices] ApplicationDbContext dbContext,
        [FromServices] IVehicleService vehicleService,
        [FromBody] AdminCreateVehicleRequest request)
    {
        try
        {
            // Get admin user ID (for authorization)
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminUserId))
            {
                return Unauthorized(new { error = "Authentication required" });
            }

            // Determine owner user ID - use provided UserId or default to admin's ID
            string ownerUserId;
            if (!string.IsNullOrWhiteSpace(request.UserId))
            {
                // Validate that the provided user ID is valid
                if (!Guid.TryParse(request.UserId, out var providedUserIdGuid))
                {
                    return BadRequest(new { error = "Invalid owner user ID" });
                }
                
                // Verify the user exists
                var ownerExists = await dbContext.Users.AnyAsync(u => u.Id == providedUserIdGuid);
                if (!ownerExists)
                {
                    return BadRequest(new { error = "Owner user not found" });
                }
                
                ownerUserId = request.UserId;
            }
            else
            {
                // Default to admin's ID if no owner specified
                ownerUserId = adminUserId;
            }

            if (!Guid.TryParse(ownerUserId, out var ownerUserIdGuid))
            {
                return BadRequest(new { error = "Invalid user ID" });
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new { error = "Title is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Brand))
            {
                return BadRequest(new { error = "Brand is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Model))
            {
                return BadRequest(new { error = "Model is required" });
            }

            if (request.Year < 1900 || request.Year > DateTime.Now.Year + 1)
            {
                return BadRequest(new { error = "Invalid year" });
            }

            if (request.Price <= 0)
            {
                return BadRequest(new { error = "Price must be greater than 0" });
            }

            if (request.Images == null || request.Images.Count == 0)
            {
                return BadRequest(new { error = "At least one image is required" });
            }

            // Determine status - default to "approved" for admin-created vehicles
            var status = string.IsNullOrWhiteSpace(request.Status) ? "approved" : request.Status.ToLower();
            if (status != "approved" && status != "pending" && status != "rejected")
            {
                status = "approved";
            }

            // Create vehicle request
            var createRequest = new CreateVehicleRequest
            {
                Title = request.Title,
                Brand = request.Brand,
                Model = request.Model,
                Year = request.Year,
                Price = request.Price,
                Type = request.Type ?? "car",
                FuelType = request.FuelType ?? "petrol",
                Transmission = request.Transmission ?? "manual",
                Condition = request.Condition ?? "USED",
                Mileage = request.Mileage,
                Description = request.Description ?? "",
                Images = request.Images,
                ContactInfo = request.ContactInfo ?? new ContactInfo()
            };

            // Use VehicleService to create the vehicle (it handles validation and subscription logic)
            // Use ownerUserId instead of adminUserId so the vehicle is owned by the specified user
            var vehicleDto = await vehicleService.CreateVehicleAsync(createRequest, ownerUserId);

            // Update status and ApprovedAt if approved
            if (status == "approved")
            {
                var vehicle = await dbContext.Vehicles.FindAsync(int.Parse(vehicleDto.Id));
                if (vehicle != null)
                {
                    vehicle.Status = "approved";
                    vehicle.ApprovedAt = DateTime.UtcNow;
                    vehicle.UpdatedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync();
                }
            }
            else if (status == "pending")
            {
                var vehicle = await dbContext.Vehicles.FindAsync(int.Parse(vehicleDto.Id));
                if (vehicle != null)
                {
                    vehicle.Status = "pending";
                    vehicle.UpdatedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync();
                }
            }

            // Reload vehicle to get updated status
            vehicleDto = await vehicleService.GetVehicleByIdAsync(vehicleDto.Id, adminUserId);
            if (vehicleDto == null)
            {
                return StatusCode(500, new { error = "Failed to retrieve created vehicle" });
            }

            return Ok(new { 
                message = "Vehicle added successfully",
                vehicle = vehicleDto
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Internal server error", 
                details = ex.Message,
                innerException = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    [HttpPut("vehicles/{vehicleId}")]
    public async Task<ActionResult> UpdateVehicle(
        string vehicleId,
        [FromBody] AdminUpdateVehicleRequest request,
        [FromServices] IVehicleService vehicleService)
    {
        try
        {
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminUserId))
            {
                return Unauthorized(new { error = "Authentication required" });
            }

            // Verify admin role
            var dbContext = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var adminUser = await dbContext.Users.FindAsync(Guid.Parse(adminUserId));
            if (adminUser == null || (adminUser.Role != "admin" && adminUser.Role != "superadmin"))
            {
                return Forbid("Admin access required");
            }

            if (!int.TryParse(vehicleId, out var vehicleIdInt))
            {
                return BadRequest(new { error = "Invalid vehicle ID" });
            }

            var vehicle = await dbContext.Vehicles
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.Id == vehicleIdInt);

            if (vehicle == null)
            {
                return NotFound(new { error = "Vehicle not found" });
            }

            // Update vehicle fields
            if (!string.IsNullOrWhiteSpace(request.Title)) vehicle.Title = request.Title;
            if (!string.IsNullOrWhiteSpace(request.Brand)) vehicle.Brand = request.Brand;
            if (!string.IsNullOrWhiteSpace(request.Model)) vehicle.Model = request.Model;
            if (request.Year.HasValue)
            {
                if (request.Year.Value < 1900 || request.Year.Value > DateTime.Now.Year + 1)
                {
                    return BadRequest(new { error = "Invalid year" });
                }
                vehicle.Year = request.Year.Value;
            }
            if (request.Price.HasValue)
            {
                if (request.Price.Value <= 0)
                {
                    return BadRequest(new { error = "Price must be greater than 0" });
                }
                vehicle.Price = request.Price.Value;
            }
            if (!string.IsNullOrWhiteSpace(request.Type)) vehicle.Type = request.Type;
            if (!string.IsNullOrWhiteSpace(request.FuelType)) vehicle.FuelType = request.FuelType;
            if (!string.IsNullOrWhiteSpace(request.Transmission)) vehicle.Transmission = request.Transmission;
            if (!string.IsNullOrWhiteSpace(request.Condition)) vehicle.Condition = request.Condition;
            if (request.Mileage.HasValue) vehicle.Mileage = request.Mileage.Value;
            if (!string.IsNullOrWhiteSpace(request.Description)) vehicle.Description = request.Description;

            // Update contact info if provided
            if (request.ContactInfo != null)
            {
                // Deserialize existing contact info
                var existingContactInfo = new ContactInfo();
                if (!string.IsNullOrEmpty(vehicle.ContactInfo))
                {
                    try
                    {
                        existingContactInfo = System.Text.Json.JsonSerializer.Deserialize<ContactInfo>(vehicle.ContactInfo) ?? new ContactInfo();
                    }
                    catch
                    {
                        existingContactInfo = new ContactInfo();
                    }
                }

                // Update with new values, keeping existing if new value is null/empty
                existingContactInfo.Phone = !string.IsNullOrWhiteSpace(request.ContactInfo.Phone) 
                    ? request.ContactInfo.Phone 
                    : existingContactInfo.Phone;
                existingContactInfo.Email = !string.IsNullOrWhiteSpace(request.ContactInfo.Email) 
                    ? request.ContactInfo.Email 
                    : existingContactInfo.Email;
                existingContactInfo.Location = !string.IsNullOrWhiteSpace(request.ContactInfo.Location) 
                    ? request.ContactInfo.Location 
                    : existingContactInfo.Location;

                // Serialize back to JSON
                vehicle.ContactInfo = System.Text.Json.JsonSerializer.Serialize(existingContactInfo);
            }

            // Update images if provided
            if (request.Images != null && request.Images.Count > 0)
            {
                vehicle.Images = System.Text.Json.JsonSerializer.Serialize(request.Images);
            }

            // Update status if provided (admins can change status)
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                var status = request.Status.ToLower();
                if (status == "approved" || status == "pending" || status == "rejected")
                {
                    vehicle.Status = status;
                    if (status == "approved" && vehicle.ApprovedAt == null)
                    {
                        vehicle.ApprovedAt = DateTime.UtcNow;
                    }
                }
            }

            vehicle.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

            // Reload vehicle to get updated data
            var vehicleDto = await vehicleService.GetVehicleByIdAsync(vehicleId, adminUserId);
            if (vehicleDto == null)
            {
                return StatusCode(500, new { error = "Failed to retrieve updated vehicle" });
            }

            return Ok(new { 
                message = "Vehicle updated successfully",
                vehicle = vehicleDto
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Internal server error", 
                details = ex.Message,
                innerException = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    [HttpGet("vehicles/table")]
    public async Task<ActionResult<VehicleTableListResponse>> GetVehiclesTable(
        [FromServices] IVehicleService vehicleService,
        [FromQuery] VehicleQueryParams queryParams)
    {
        try
        {
            var response = await vehicleService.GetVehiclesTableAsync(queryParams);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Internal server error", 
                details = ex.Message
            });
        }
    }

    [HttpGet("vehicles")]
    public async Task<ActionResult> GetAllVehicles([FromServices] IVehicleService vehicleService, [FromQuery] string? status = null)
    {
        try
        {
            var dbContext = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Build optimized query with projection and joins
            var query = from v in dbContext.Vehicles
                       join u in dbContext.Users on v.UserId equals u.Id into userJoin
                       from user in userJoin.DefaultIfEmpty()
                       select new
                       {
                           Vehicle = v,
                           User = user,
                           UserId = v.UserId
                       };

            // Apply status filter if provided and not "all"
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                query = query.Where(x => x.Vehicle.Status == status);
            }

            // Get distinct user IDs for premium check (before pagination for efficiency)
            var userIds = await query
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync();

            // Get premium user IDs in a single query
            var premiumUserIds = userIds.Any() ? await dbContext.Subscriptions
                .Where(s => userIds.Contains(s.UserId) &&
                           s.Status == "active" &&
                           s.EndDate > DateTime.UtcNow)
                .Select(s => s.UserId)
                .ToListAsync() : new List<Guid>();

            // Apply sorting at database level and get results
            var results = await query
                .OrderByDescending(x => x.Vehicle.CreatedAt)
                .ThenByDescending(x => x.Vehicle.IsPremium)
                .ThenByDescending(x => x.Vehicle.ApprovedAt ?? DateTime.MinValue)
                .AsNoTracking()
                .ToListAsync();

            // Map to DTOs efficiently
            var vehicleDtos = results.Select(x =>
            {
                var v = x.Vehicle;
                var isPremiumUser = premiumUserIds.Contains(x.UserId);
                
                // Handle null safety for Images
                var images = new List<string>();
                if (!string.IsNullOrEmpty(v.Images))
                {
                    try
                    {
                        images = System.Text.Json.JsonSerializer.Deserialize<List<string>>(v.Images) ?? new List<string>();
                    }
                    catch { }
                }

                // Handle null safety for ContactInfo
                var contactInfo = new ContactInfo();
                if (!string.IsNullOrEmpty(v.ContactInfo))
                {
                    try
                    {
                        contactInfo = System.Text.Json.JsonSerializer.Deserialize<ContactInfo>(v.ContactInfo) ?? new ContactInfo();
                    }
                    catch { }
                }

                // Determine if email should be exposed (admins always see emails)
                var shouldExposeEmail = true; // Admins should always see owner emails

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
            
            return Ok(new { vehicles = vehicleDtos });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", details = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    private VehicleDto MapToVehicleDto(Models.Vehicle vehicle, bool isPremiumUser, string? currentUserId)
    {
        // Handle null safety for Images
        var images = new List<string>();
        try
        {
            if (!string.IsNullOrEmpty(vehicle.Images))
            {
                images = System.Text.Json.JsonSerializer.Deserialize<List<string>>(vehicle.Images) ?? new List<string>();
            }
        }
        catch 
        { 
            images = new List<string>();
        }

        // Handle null safety for ContactInfo
        var contactInfo = new ContactInfo();
        try
        {
            if (!string.IsNullOrEmpty(vehicle.ContactInfo))
            {
                contactInfo = System.Text.Json.JsonSerializer.Deserialize<ContactInfo>(vehicle.ContactInfo) ?? new ContactInfo();
            }
        }
        catch 
        { 
            contactInfo = new ContactInfo();
        }

        // Handle null safety for User
        // For admin endpoints, always expose email since admins need to see owner information
        // Since this is AdminController, the currentUserId is always an admin, so always expose emails
        var shouldExposeEmail = true; // Admins should always see owner emails

        return new VehicleDto
        {
            Id = vehicle.Id.ToString(),
            Title = vehicle.Title ?? string.Empty,
            Brand = vehicle.Brand ?? string.Empty,
            Model = vehicle.Model ?? string.Empty,
            Year = vehicle.Year,
            Price = vehicle.Price,
            Type = vehicle.Type ?? string.Empty,
            FuelType = vehicle.FuelType ?? string.Empty,
            Transmission = vehicle.Transmission ?? string.Empty,
            Condition = vehicle.Condition ?? string.Empty,
            Mileage = vehicle.Mileage,
            Description = vehicle.Description ?? string.Empty,
            Images = images,
            ContactInfo = contactInfo,
            Status = vehicle.Status ?? "pending",
            UserId = vehicle.UserId.ToString(),
            CreatedAt = vehicle.CreatedAt,
            UpdatedAt = vehicle.UpdatedAt ?? DateTime.UtcNow,
            ApprovedAt = vehicle.ApprovedAt,
            IsPremium = vehicle.IsPremium,
            IsPremiumUser = isPremiumUser,
            User = vehicle.User != null ? new UserInfo
            {
                Id = vehicle.User.Id.ToString(),
                Name = vehicle.User.Name ?? string.Empty,
                Email = shouldExposeEmail ? vehicle.User.Email : null
            } : null
        };
    }

    [HttpPost("users/{userId}/toggle-block")]
    public async Task<ActionResult> ToggleUserBlock(string userId, [FromBody] ToggleUserBlockRequest? request = null)
    {
        try
        {
            // Support both route parameter and body parameter for flexibility
            var targetUserId = userId;
            bool? blockValue = null;

            if (request != null)
            {
                // If body is provided, use it (frontend sends userId and block in body)
                if (!string.IsNullOrEmpty(request.UserId))
                {
                    targetUserId = request.UserId;
                }
                blockValue = request.Block;
            }

            var isBlocked = await _adminService.ToggleUserBlockAsync(targetUserId);
            
            // If block value is explicitly provided in request, use it instead of toggling
            if (blockValue.HasValue)
            {
                var dbContext = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                if (!Guid.TryParse(targetUserId, out var userIdGuid))
                {
                    return BadRequest(new { error = "Invalid user ID" });
                }

                var user = await dbContext.Users.FindAsync(userIdGuid);
                if (user == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                user.IsBlocked = blockValue.Value;
                user.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
                isBlocked = blockValue.Value;
            }

            return Ok(new { message = $"User {(isBlocked ? "blocked" : "unblocked")} successfully", isBlocked });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpPut("users/{userId}")]
    public async Task<ActionResult> UpdateUser(string userId, [FromBody] UpdateUserRequest request)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userIdGuid))
            {
                return BadRequest(new { error = "Invalid user ID" });
            }

            var dbContext = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var user = await dbContext.Users.FindAsync(userIdGuid);

            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Prevent editing admins/superadmins (only allow editing regular users)
            if (user.Role != "user")
            {
                return BadRequest(new { error = "Cannot edit admin users. Use admin management instead." });
            }

            // Update fields
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                user.Name = request.Name.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                // Check if email is already taken by another user
                var existingUser = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email.Trim().ToLower() && u.Id != userIdGuid);
                
                if (existingUser != null)
                {
                    return Conflict(new { error = "Email is already in use" });
                }

                user.Email = request.Email.Trim().ToLower();
            }

            if (request.IsBlocked.HasValue)
            {
                user.IsBlocked = request.IsBlocked.Value;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

            var userDto = new UserDto
            {
                Id = user.Id.ToString(),
                Email = user.Email ?? string.Empty,
                Name = user.Name ?? string.Empty,
                Role = user.Role ?? "user",
                IsBlocked = user.IsBlocked,
                Phone = user.Phone,
                LastLogin = user.LastLogin,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return Ok(new { 
                message = "User updated successfully",
                user = userDto
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Internal server error", 
                details = ex.Message
            });
        }
    }

    [HttpDelete("users/delete")]
    public async Task<ActionResult> DeleteUser([FromBody] DeleteUserRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return BadRequest(new { error = "User ID is required" });
            }

            if (!Guid.TryParse(request.UserId, out var userIdGuid))
            {
                return BadRequest(new { error = "Invalid user ID" });
            }

            var dbContext = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var user = await dbContext.Users.FindAsync(userIdGuid);

            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Prevent deleting admins/superadmins (only allow deleting regular users)
            if (user.Role != "user")
            {
                return BadRequest(new { error = "Cannot delete admin users. Use admin management instead." });
            }

            // Prevent deleting yourself
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == user.Id.ToString())
            {
                return BadRequest(new { error = "You cannot delete yourself" });
            }

            // Delete related records first
            var vehicles = await dbContext.Vehicles
                .Where(v => v.UserId == userIdGuid)
                .ToListAsync();
            
            if (vehicles.Any())
            {
                var vehicleIds = vehicles.Select(v => v.Id).ToList();
                
                // Delete favorites for these vehicles
                var favorites = await dbContext.Favorites
                    .Where(f => vehicleIds.Contains(f.VehicleId))
                    .ToListAsync();
                if (favorites.Any())
                {
                    dbContext.Favorites.RemoveRange(favorites);
                }

                // Delete vehicle images
                var vehicleImages = await dbContext.VehicleImages
                    .Where(vi => vehicleIds.Contains(vi.VehicleId))
                    .ToListAsync();
                if (vehicleImages.Any())
                {
                    dbContext.VehicleImages.RemoveRange(vehicleImages);
                }

                // Delete vehicles
                dbContext.Vehicles.RemoveRange(vehicles);
            }

            // Delete user's favorites
            var userFavorites = await dbContext.Favorites
                .Where(f => f.UserId == userIdGuid)
                .ToListAsync();
            if (userFavorites.Any())
            {
                dbContext.Favorites.RemoveRange(userFavorites);
            }

            // Delete user's subscriptions
            var subscriptions = await dbContext.Subscriptions
                .Where(s => s.UserId == userIdGuid)
                .ToListAsync();
            if (subscriptions.Any())
            {
                dbContext.Subscriptions.RemoveRange(subscriptions);
            }

            // Delete the user
            dbContext.Users.Remove(user);
            await dbContext.SaveChangesAsync();

            return Ok(new { message = "User deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Internal server error", 
                details = ex.Message
            });
        }
    }

    [HttpPost("vehicles/{vehicleId}/approve")]
    public async Task<ActionResult> ApproveVehicle(string vehicleId)
    {
        try
        {
            await _adminService.ApproveVehicleAsync(vehicleId);
            return Ok(new { message = "Vehicle approved successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("vehicles/{vehicleId}/reject")]
    public async Task<ActionResult> RejectVehicle(string vehicleId)
    {
        try
        {
            await _adminService.RejectVehicleAsync(vehicleId);
            return Ok(new { message = "Vehicle rejected successfully" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("vehicles/bulk-approve")]
    public async Task<ActionResult> BulkApproveVehicles([FromBody] List<string> vehicleIds)
    {
        try
        {
            await _adminService.BulkApproveVehiclesAsync(vehicleIds);
            return Ok(new { message = "Vehicles approved successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpDelete("vehicles/{vehicleId}")]
    public async Task<ActionResult> DeleteVehicle(string vehicleId)
    {
        try
        {
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminUserId))
            {
                return Unauthorized(new { error = "Authentication required" });
            }

            // Verify admin role
            var dbContext = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var adminUser = await dbContext.Users.FindAsync(Guid.Parse(adminUserId));
            if (adminUser == null || (adminUser.Role != "admin" && adminUser.Role != "superadmin"))
            {
                return Forbid("Admin access required");
            }

            if (string.IsNullOrWhiteSpace(vehicleId))
            {
                return BadRequest(new { error = "Vehicle ID is required" });
            }

            if (!int.TryParse(vehicleId, out var vehicleIdInt))
            {
                return BadRequest(new { error = "Invalid vehicle ID" });
            }

            var vehicle = await dbContext.Vehicles.FindAsync(vehicleIdInt);
            if (vehicle == null)
            {
                return NotFound(new { error = "Vehicle not found" });
            }

            // Use a transaction to ensure atomicity
            using var transaction = await dbContext.Database.BeginTransactionAsync();
            try
            {
                // Delete related records first to avoid foreign key constraint issues
                // Delete all favorites for this vehicle
                var favorites = await dbContext.Favorites
                    .Where(f => f.VehicleId == vehicleIdInt)
                    .ToListAsync();
                if (favorites.Any())
                {
                    dbContext.Favorites.RemoveRange(favorites);
                    await dbContext.SaveChangesAsync();
                }

                // Delete all vehicle images for this vehicle (if using separate table)
                var vehicleImages = await dbContext.VehicleImages
                    .Where(vi => vi.VehicleId == vehicleIdInt)
                    .ToListAsync();
                if (vehicleImages.Any())
                {
                    dbContext.VehicleImages.RemoveRange(vehicleImages);
                    await dbContext.SaveChangesAsync();
                }

                // Now delete the vehicle itself
                dbContext.Vehicles.Remove(vehicle);
                await dbContext.SaveChangesAsync();

                // Commit the transaction
                await transaction.CommitAsync();

                return Ok(new { message = "Vehicle deleted successfully" });
            }
            catch
            {
                // Rollback on error
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Internal server error", 
                details = ex.Message,
                innerException = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    [HttpPost("vehicles/bulk-delete")]
    public async Task<ActionResult> BulkDeleteVehicles([FromBody] List<string> vehicleIds)
    {
        try
        {
            await _adminService.BulkDeleteVehiclesAsync(vehicleIds);
            return Ok(new { message = "Vehicles deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsDto>> GetStats([FromServices] IStatsService statsService)
    {
        try
        {
            var stats = await statsService.GetAdminStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpGet("user-reports")]
    public async Task<ActionResult> GetUserReports(
        [FromServices] ApplicationDbContext dbContext,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? registeredDateFrom = null,
        [FromQuery] string? registeredDateTo = null,
        [FromQuery] string? userType = null,
        [FromQuery] int? vehicleCountMin = null,
        [FromQuery] int? vehicleCountMax = null,
        [FromQuery] string? lastLoginFrom = null,
        [FromQuery] string? lastLoginTo = null,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortOrder = "desc",
        [FromQuery] bool export = false)
    {
        try
        {
            // Validate inputs
            if (page < 1) page = 1;
            if (limit < 1) limit = 10;
            if (limit > 100) limit = 100; // Cap at 100 for performance
            
            // Start with a simple query
            var query = dbContext.Users.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                if (status == "blocked")
                    query = query.Where(u => u.IsBlocked);
                else if (status == "active")
                    query = query.Where(u => !u.IsBlocked);
            }

            if (!string.IsNullOrEmpty(userType) && userType != "all")
            {
                query = query.Where(u => u.Role == userType);
            }

            if (DateTime.TryParse(registeredDateFrom, out var regFrom))
            {
                query = query.Where(u => u.CreatedAt >= regFrom);
            }

            if (DateTime.TryParse(registeredDateTo, out var regTo))
            {
                query = query.Where(u => u.CreatedAt <= regTo);
            }

            if (DateTime.TryParse(lastLoginFrom, out var loginFrom))
            {
                query = query.Where(u => u.LastLogin != null && u.LastLogin >= loginFrom);
            }

            if (DateTime.TryParse(lastLoginTo, out var loginTo))
            {
                query = query.Where(u => u.LastLogin != null && u.LastLogin <= loginTo);
            }

            // Get all users first to calculate vehicle counts
            var allUsers = await query.ToListAsync();
            var userIds = allUsers.Select(u => u.Id).ToList();

            // Initialize dictionaries with empty defaults
            var vehicleCounts = new Dictionary<Guid, int>();
            var vehicleStatusDict = new Dictionary<Guid, (int Approved, int Pending, int Rejected)>();
            var favoritesCounts = new Dictionary<Guid, int>();
            var subscriptionDict = new Dictionary<Guid, Subscription>();
            var planDict = new Dictionary<string, string>();

            if (userIds.Any())
            {
                try
                {
                    // Get vehicle counts per user
                    var vehicleCountsList = await dbContext.Vehicles
                        .Where(v => userIds.Contains(v.UserId))
                        .GroupBy(v => v.UserId)
                        .Select(g => new { UserId = g.Key, Count = g.Count() })
                        .ToListAsync();

                    vehicleCounts = vehicleCountsList.ToDictionary(x => x.UserId, x => x.Count);

                    // Get vehicle status counts - handle null Status
                    var vehicleStatusCounts = await dbContext.Vehicles
                        .Where(v => userIds.Contains(v.UserId))
                        .GroupBy(v => new { v.UserId, Status = v.Status ?? "pending" })
                        .Select(g => new { g.Key.UserId, g.Key.Status, Count = g.Count() })
                        .ToListAsync();

                    vehicleStatusDict = vehicleStatusCounts
                        .GroupBy(v => v.UserId)
                        .ToDictionary(
                            g => g.Key,
                            g => (
                                Approved: g.Where(x => x.Status == "approved").Sum(x => x.Count),
                                Pending: g.Where(x => x.Status == "pending").Sum(x => x.Count),
                                Rejected: g.Where(x => x.Status == "rejected").Sum(x => x.Count)
                            )
                        );

                    // Get favorites counts
                    var favoritesCountsList = await dbContext.Favorites
                        .Where(f => userIds.Contains(f.UserId))
                        .GroupBy(f => f.UserId)
                        .Select(g => new { UserId = g.Key, Count = g.Count() })
                        .ToListAsync();

                    favoritesCounts = favoritesCountsList.ToDictionary(x => x.UserId, x => x.Count);

                    // Get active subscriptions
                    var activeSubscriptions = await dbContext.Subscriptions
                        .Where(s => userIds.Contains(s.UserId) && s.Status == "active" && s.EndDate > DateTime.UtcNow)
                        .ToListAsync();

                    subscriptionDict = activeSubscriptions
                        .GroupBy(s => s.UserId)
                        .ToDictionary(
                            g => g.Key,
                            g => g.OrderByDescending(s => s.EndDate).First()
                        );
                }
                catch (Exception queryEx)
                {
                    // Log query error but continue with empty dictionaries
                    Console.WriteLine($"Error loading user report data: {queryEx.Message}");
                }
            }

            // Get subscription plans for plan names
            try
            {
                var plans = await dbContext.SubscriptionPlans.ToListAsync();
                planDict = plans
                    .Where(p => !string.IsNullOrEmpty(p.PlanType))
                    .ToDictionary(
                        p => p.PlanType!, 
                        p => !string.IsNullOrEmpty(p.Name) ? p.Name : p.PlanType!
                    );
            }
            catch
            {
                // Continue with empty planDict
            }

            // Build user reports with extended data
            var userReports = allUsers.Select(u =>
            {
                var hasVehicleStatus = vehicleStatusDict.TryGetValue(u.Id, out var vehicleStatus);
                var hasSubscription = subscriptionDict.TryGetValue(u.Id, out var subscription);
                
                var approved = hasVehicleStatus ? vehicleStatus.Approved : 0;
                var pending = hasVehicleStatus ? vehicleStatus.Pending : 0;
                var rejected = hasVehicleStatus ? vehicleStatus.Rejected : 0;
                
                string subscriptionPlan = "";
                string subscriptionEndDate = "";
                
                if (hasSubscription && subscription != null)
                {
                    var planType = subscription.PlanType ?? "";
                    subscriptionPlan = planDict.GetValueOrDefault(planType) ?? planType;
                    subscriptionEndDate = subscription.EndDate.ToString("yyyy-MM-ddTHH:mm:ss");
                }
                
                return new
                {
                    id = u.Id.ToString(),
                    name = u.Name ?? "",
                    email = u.Email ?? "",
                    phone = u.Phone ?? "",
                    isBlocked = u.IsBlocked,
                    lastLogin = u.LastLogin?.ToString("yyyy-MM-ddTHH:mm:ss") ?? "",
                    createdAt = u.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    updatedAt = u.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    vehicleCount = vehicleCounts.GetValueOrDefault(u.Id, 0),
                    approvedVehicles = approved,
                    pendingVehicles = pending,
                    rejectedVehicles = rejected,
                    favoritesCount = favoritesCounts.GetValueOrDefault(u.Id, 0),
                    hasActiveSubscription = hasSubscription,
                    subscriptionPlan = subscriptionPlan,
                    subscriptionEndDate = subscriptionEndDate
                };
            }).ToList();

            // Apply vehicle count filter
            if (vehicleCountMin.HasValue)
            {
                userReports = userReports.Where(u => u.vehicleCount >= vehicleCountMin.Value).ToList();
            }

            if (vehicleCountMax.HasValue)
            {
                userReports = userReports.Where(u => u.vehicleCount <= vehicleCountMax.Value).ToList();
            }

            // Apply sorting
            if (sortBy == "createdAt")
            {
                userReports = sortOrder == "asc"
                    ? userReports.OrderBy(u => u.createdAt).ToList()
                    : userReports.OrderByDescending(u => u.createdAt).ToList();
            }
            else if (sortBy == "name")
            {
                userReports = sortOrder == "asc"
                    ? userReports.OrderBy(u => u.name).ToList()
                    : userReports.OrderByDescending(u => u.name).ToList();
            }
            else if (sortBy == "vehicleCount")
            {
                userReports = sortOrder == "asc"
                    ? userReports.OrderBy(u => u.vehicleCount).ToList()
                    : userReports.OrderByDescending(u => u.vehicleCount).ToList();
            }

            // Pagination (unless export)
            var total = userReports.Count;
            var users = export 
                ? userReports 
                : userReports.Skip((page - 1) * limit).Take(limit).ToList();

            // Get recent activities (recent user registrations and logins)
            var recentActivitiesList = new List<object>();
            try
            {
                var activities = await dbContext.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(20)
                    .Select(u => new
                    {
                        activityType = "registration",
                        userName = u.Name ?? "",
                        userEmail = u.Email ?? "",
                        activityDate = u.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                        description = $"User {(u.Name ?? "Unknown")} registered",
                        userPhone = u.Phone ?? "",
                        userStatus = !u.IsBlocked
                    })
                    .ToListAsync();
                
                recentActivitiesList = activities.Select(a => (object)a).ToList();
            }
            catch
            {
                // Continue with empty list
            }
            
            var recentActivities = recentActivitiesList;

            // Calculate analytics
            object analytics;
            object[] statusDistribution;
            object statistics;
            
            try
            {
                var now = DateTime.UtcNow;
                var weekAgo = now.AddDays(-7);
                var monthAgo = now.AddDays(-30);

                var newUsersThisWeek = await dbContext.Users.CountAsync(u => u.CreatedAt >= weekAgo);
                var newUsersThisMonth = await dbContext.Users.CountAsync(u => u.CreatedAt >= monthAgo);
                var activeToday = await dbContext.Users.CountAsync(u => u.LastLogin != null && u.LastLogin.Value.Date == now.Date);
                var activeThisWeek = await dbContext.Users.CountAsync(u => u.LastLogin != null && u.LastLogin >= weekAgo);
                var activeThisMonth = await dbContext.Users.CountAsync(u => u.LastLogin != null && u.LastLogin >= monthAgo);

                var totalVehicles = await dbContext.Vehicles.CountAsync();
                var totalUsers = await dbContext.Users.CountAsync();
                var avgVehiclesPerUser = totalUsers > 0 ? (double)totalVehicles / totalUsers : 0;

                analytics = new
                {
                    newUsersThisWeek,
                    newUsersThisMonth,
                    activeToday,
                    activeThisWeek,
                    activeThisMonth,
                    avgVehiclesPerUser = Math.Round(avgVehiclesPerUser, 2)
                };

                // Status distribution
                var activeCount = await dbContext.Users.CountAsync(u => !u.IsBlocked);
                var blockedCount = await dbContext.Users.CountAsync(u => u.IsBlocked);
                statusDistribution = new object[]
                {
                    new { status = "active", count = activeCount },
                    new { status = "blocked", count = blockedCount }
                };

                // Statistics
                var totalActiveSubscriptions = await dbContext.Subscriptions.CountAsync(s => s.Status == "active" && s.EndDate > DateTime.UtcNow);
                statistics = new
                {
                    totalUsers = totalUsers,
                    totalVehicles = totalVehicles,
                    totalActiveSubscriptions = totalActiveSubscriptions
                };
            }
            catch (Exception analyticsEx)
            {
                // Return default values if analytics fail
                analytics = new
                {
                    newUsersThisWeek = 0,
                    newUsersThisMonth = 0,
                    activeToday = 0,
                    activeThisWeek = 0,
                    activeThisMonth = 0,
                    avgVehiclesPerUser = 0.0
                };
                statusDistribution = new object[]
                {
                    new { status = "active", count = 0 },
                    new { status = "blocked", count = 0 }
                };
                statistics = new
                {
                    totalUsers = 0,
                    totalVehicles = 0,
                    totalActiveSubscriptions = 0
                };
            }

            return Ok(new
            {
                users,
                recentActivities,
                analytics,
                statusDistribution,
                statistics,
                pagination = export ? null : new
                {
                    page,
                    limit,
                    total,
                    totalPages = (int)Math.Ceiling((double)total / limit)
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Internal server error", 
                details = ex.Message,
                innerException = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    [HttpGet("vehicle-analytics")]
    public async Task<ActionResult> GetVehicleAnalytics(
        [FromServices] ApplicationDbContext dbContext,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? brand = null,
        [FromQuery] string? type = null,
        [FromQuery] decimal? priceMin = null,
        [FromQuery] decimal? priceMax = null,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortOrder = "desc",
        [FromQuery] bool export = false)
    {
        try
        {
            // Validate inputs
            if (page < 1) page = 1;
            if (limit < 1) limit = 10;
            if (limit > 100) limit = 100;

            if (dbContext == null)
            {
                return StatusCode(500, new { error = "Database context is not available" });
            }

            var query = dbContext.Vehicles
                .Include(v => v.User)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                query = query.Where(v => v.Status == status);
            }

            if (!string.IsNullOrEmpty(brand) && brand != "all")
            {
                query = query.Where(v => v.Brand == brand);
            }

            if (!string.IsNullOrEmpty(type) && type != "all")
            {
                query = query.Where(v => v.Type == type);
            }

            if (priceMin.HasValue)
            {
                query = query.Where(v => v.Price >= priceMin.Value);
            }

            if (priceMax.HasValue)
            {
                query = query.Where(v => v.Price <= priceMax.Value);
            }

            if (DateTime.TryParse(dateFrom, out var fromDate))
            {
                query = query.Where(v => v.CreatedAt >= fromDate);
            }

            if (DateTime.TryParse(dateTo, out var toDate))
            {
                query = query.Where(v => v.CreatedAt <= toDate);
            }

            // Get total count for pagination (before loading data)
            var total = await query.CountAsync();

            // Apply sorting at database level
            IQueryable<Models.Vehicle> sortedQuery = sortBy switch
            {
                "price" => sortOrder == "asc" 
                    ? query.OrderBy(v => v.Price) 
                    : query.OrderByDescending(v => v.Price),
                "title" => sortOrder == "asc" 
                    ? query.OrderBy(v => v.Title) 
                    : query.OrderByDescending(v => v.Title),
                _ => sortOrder == "asc" 
                    ? query.OrderBy(v => v.CreatedAt) 
                    : query.OrderByDescending(v => v.CreatedAt)
            };

            // For export/reports, use optimized projection to avoid loading heavy fields
            // For regular pagination, load entities but still optimize
            List<object> vehicles;
            
            if (export)
            {
                // For export: use lightweight projection directly from database (no Images, Description, ContactInfo)
                var vehiclesQuery = sortedQuery
                    .Select(v => new
                    {
                        v.Id,
                        v.Title,
                        v.Brand,
                        v.Model,
                        v.Type,
                        v.Year,
                        v.Price,
                        v.Status,
                        v.CreatedAt,
                        v.UpdatedAt,
                        OwnerName = v.User != null ? v.User.Name : "",
                        OwnerEmail = v.User != null ? v.User.Email : "",
                        OwnerPhone = v.User != null ? (v.User.Phone ?? "") : ""
                    });

                var vehiclesData = await vehiclesQuery.ToListAsync();
                
                // Get favorites counts for all vehicles in one query
                var allVehicleIds = vehiclesData.Select(v => v.Id).ToList();
                var favoritesCounts = new Dictionary<int, int>();
                
                if (allVehicleIds.Any())
                {
                    try
                    {
                        var favoritesList = await dbContext.Favorites
                            .Where(f => allVehicleIds.Contains(f.VehicleId))
                            .GroupBy(f => f.VehicleId)
                            .Select(g => new { VehicleId = g.Key, Count = g.Count() })
                            .ToListAsync();

                        favoritesCounts = favoritesList.ToDictionary(x => x.VehicleId, x => x.Count);
                    }
                    catch
                    {
                        // Continue with empty dictionary
                    }
                }

                // Build response with favorites count
                vehicles = vehiclesData.Select(v => (object)new
                {
                    id = v.Id,
                    title = v.Title ?? "",
                    brand = v.Brand ?? "",
                    model = v.Model ?? "",
                    type = v.Type ?? "",
                    year = v.Year,
                    price = v.Price,
                    status = v.Status ?? "pending",
                    createdAt = v.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    updatedAt = v.UpdatedAt?.ToString("yyyy-MM-ddTHH:mm:ss") ?? "",
                    ownerName = v.OwnerName,
                    ownerEmail = v.OwnerEmail,
                    ownerPhone = v.OwnerPhone,
                    favoritesCount = favoritesCounts.GetValueOrDefault(v.Id, 0)
                }).ToList();
            }
            else
            {
                // For regular pagination: load only paginated vehicles
                var vehiclesToLoad = await sortedQuery
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();

                // Get favorites counts only for loaded vehicles
                var vehicleIds = vehiclesToLoad.Select(v => v.Id).ToList();
            var favoritesCounts = new Dictionary<int, int>();
            
            if (vehicleIds.Any())
            {
                try
                {
                    var favoritesList = await dbContext.Favorites
                        .Where(f => vehicleIds.Contains(f.VehicleId))
                        .GroupBy(f => f.VehicleId)
                        .Select(g => new { VehicleId = g.Key, Count = g.Count() })
                        .ToListAsync();

                    favoritesCounts = favoritesList.ToDictionary(x => x.VehicleId, x => x.Count);
                }
                catch
                {
                    // Continue with empty dictionary
                }
            }

                // Build vehicle reports with extended data (only for loaded vehicles)
                vehicles = vehiclesToLoad.Select(v =>
            {
                var owner = v.User;
                    return (object)new
                {
                    id = v.Id,
                    title = v.Title ?? "",
                    brand = v.Brand ?? "",
                    model = v.Model ?? "",
                    type = v.Type ?? "",
                    year = v.Year,
                    price = v.Price,
                    status = v.Status ?? "pending",
                    createdAt = v.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    updatedAt = v.UpdatedAt?.ToString("yyyy-MM-ddTHH:mm:ss") ?? "",
                    ownerName = owner?.Name ?? "",
                    ownerEmail = owner?.Email ?? "",
                    ownerPhone = owner?.Phone ?? "",
                    favoritesCount = favoritesCounts.GetValueOrDefault(v.Id, 0)
                };
            }).ToList();
            }

            // Get recent activities (recent vehicle creations and approvals)
            var recentActivitiesList = new List<object>();
            try
            {
                var activities = await dbContext.Vehicles
                    .Include(v => v.User)
                    .OrderByDescending(v => v.CreatedAt)
                    .Take(20)
                    .Select(v => new
                    {
                        activityType = v.Status == "approved" ? "approval" : "creation",
                        description = v.Status == "approved" 
                            ? $"Vehicle {v.Title} was approved"
                            : $"Vehicle {v.Title} was created",
                        userName = v.User != null ? v.User.Name : "",
                        userEmail = v.User != null ? v.User.Email : "",
                        userPhone = v.User != null ? (v.User.Phone ?? "") : "",
                        status = v.Status ?? "pending",
                        activityDate = v.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss")
                    })
                    .ToListAsync();

                recentActivitiesList = activities.Select(a => (object)a).ToList();
            }
            catch
            {
                // Continue with empty list
            }

            // Calculate analytics
            object analytics;
            object[] statusDistribution;
            object[] brandDistribution;
            object[] typeDistribution;
            object[] priceRangeDistribution;
            object statistics;

            try
            {
                // Use database aggregations instead of loading all vehicles
                var now = DateTime.UtcNow;
                var weekAgo = now.AddDays(-7);
                var monthAgo = now.AddDays(-30);
                var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
                var todayEnd = todayStart.AddDays(1);

                // Calculate analytics using database queries (much faster)
                var newVehiclesThisWeek = await dbContext.Vehicles
                    .CountAsync(v => v.CreatedAt >= weekAgo);
                var newVehiclesThisMonth = await dbContext.Vehicles
                    .CountAsync(v => v.CreatedAt >= monthAgo);
                var newVehiclesToday = await dbContext.Vehicles
                    .CountAsync(v => v.CreatedAt >= todayStart && v.CreatedAt < todayEnd);
                var approvedThisWeek = await dbContext.Vehicles
                    .CountAsync(v => (v.Status ?? "pending") == "approved" && 
                                    v.ApprovedAt != null && 
                                    v.ApprovedAt.Value >= weekAgo);
                var pendingVehicles = await dbContext.Vehicles
                    .CountAsync(v => (v.Status ?? "pending") == "pending");

                // Get average price - include all vehicles with price >= 0 (not just > 0)
                var avgPriceResult = await dbContext.Vehicles
                    .Where(v => v.Price >= 0)
                    .Select(v => (double?)v.Price)
                    .AverageAsync();
                var avgPrice = avgPriceResult ?? 0.0;

                var uniqueBrands = await dbContext.Vehicles
                    .Where(v => !string.IsNullOrEmpty(v.Brand))
                    .Select(v => v.Brand)
                    .Distinct()
                    .CountAsync();

                var uniqueTypes = await dbContext.Vehicles
                    .Where(v => !string.IsNullOrEmpty(v.Type))
                    .Select(v => v.Type)
                    .Distinct()
                    .CountAsync();

                analytics = new
                {
                    newVehiclesThisWeek,
                    newVehiclesThisMonth,
                    newVehiclesToday,
                    approvedThisWeek,
                    pendingVehicles,
                    avgPrice = Math.Round(avgPrice, 2),
                    uniqueBrands,
                    uniqueTypes
                };

                // Status distribution - use database aggregation
                var statusCounts = await dbContext.Vehicles
                    .GroupBy(v => v.Status ?? "pending")
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                var totalForStatus = statusCounts.Sum(s => s.Count);
                statusDistribution = statusCounts
                    .Select(s => (object)new { 
                        status = s.Status, 
                        count = s.Count, 
                        percentage = totalForStatus > 0 ? Math.Round((double)s.Count / totalForStatus * 100, 2) : 0 
                    })
                    .ToArray();

                // Brand distribution (top 10) - use database aggregation
                var brandCounts = await dbContext.Vehicles
                    .Where(v => !string.IsNullOrEmpty(v.Brand))
                    .GroupBy(v => v.Brand)
                    .Select(g => new { Brand = g.Key, Count = g.Count() })
                    .OrderByDescending(b => b.Count)
                    .Take(10)
                    .ToListAsync();

                var totalForBrands = brandCounts.Sum(b => b.Count);
                brandDistribution = brandCounts
                    .Select(b => (object)new { 
                        brand = b.Brand, 
                        count = b.Count, 
                        percentage = totalForBrands > 0 ? Math.Round((double)b.Count / totalForBrands * 100, 2) : 0 
                    })
                    .ToArray();

                // Type distribution - use database aggregation
                var typeCounts = await dbContext.Vehicles
                    .Where(v => !string.IsNullOrEmpty(v.Type))
                    .GroupBy(v => v.Type)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToListAsync();

                var totalForTypes = typeCounts.Sum(t => t.Count);
                typeDistribution = typeCounts
                    .Select(t => (object)new { 
                        type = t.Type, 
                        count = t.Count, 
                        percentage = totalForTypes > 0 ? Math.Round((double)t.Count / totalForTypes * 100, 2) : 0 
                    })
                    .ToArray();

                // Price range distribution - use database aggregations
                var priceRanges = new[]
                {
                    new { Min = 0m, Max = 500000m, Label = "0-500K" },
                    new { Min = 500000m, Max = 1000000m, Label = "500K-1M" },
                    new { Min = 1000000m, Max = 2000000m, Label = "1M-2M" },
                    new { Min = 2000000m, Max = 5000000m, Label = "2M-5M" },
                    new { Min = 5000000m, Max = decimal.MaxValue, Label = "5M+" }
                };

                var totalVehiclesForPrice = await dbContext.Vehicles.CountAsync();
                var priceRangeCounts = new List<object>();

                foreach (var range in priceRanges)
                {
                    var count = await dbContext.Vehicles
                        .CountAsync(v => v.Price >= range.Min && v.Price < range.Max);
                    var percentage = totalVehiclesForPrice > 0 
                        ? Math.Round((double)count / totalVehiclesForPrice * 100, 2) 
                        : 0;
                    priceRangeCounts.Add(new { priceRange = range.Label, count, percentage });
                }

                priceRangeDistribution = priceRangeCounts.ToArray();

                // Statistics - use database aggregations
                var totalVehicles = await dbContext.Vehicles.CountAsync();
                var totalApproved = await dbContext.Vehicles
                    .CountAsync(v => (v.Status ?? "pending") == "approved");
                var totalPending = await dbContext.Vehicles
                    .CountAsync(v => (v.Status ?? "pending") == "pending");
                var totalRejected = await dbContext.Vehicles
                    .CountAsync(v => (v.Status ?? "pending") == "rejected");

                // Get price statistics using database queries - include all vehicles with price >= 0
                var minPriceResult = await dbContext.Vehicles
                    .Where(v => v.Price >= 0)
                    .Select(v => (decimal?)v.Price)
                    .MinAsync();
                var maxPriceResult = await dbContext.Vehicles
                    .Where(v => v.Price >= 0)
                    .Select(v => (decimal?)v.Price)
                    .MaxAsync();
                var minPrice = minPriceResult ?? 0m;
                var maxPrice = maxPriceResult ?? 0m;

                statistics = new
                {
                    totalVehicles,
                    approvedVehicles = totalApproved, // Match frontend expectation
                    pendingVehicles = totalPending, // Match frontend expectation
                    rejectedVehicles = totalRejected, // Match frontend expectation
                    avgPrice = avgPrice, // From analytics above
                    minPrice = (double)minPrice,
                    maxPrice = (double)maxPrice
                };
            }
            catch (Exception analyticsEx)
            {
                // Log the error for debugging
                Console.WriteLine($"Error calculating vehicle analytics: {analyticsEx.Message}");
                Console.WriteLine($"Stack trace: {analyticsEx.StackTrace}");
                if (analyticsEx.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {analyticsEx.InnerException.Message}");
                }
                
                // Try to return at least basic statistics even if detailed analytics fail
                try
                {
                    var basicTotal = await dbContext.Vehicles.CountAsync();
                    var basicApproved = await dbContext.Vehicles.CountAsync(v => (v.Status ?? "pending") == "approved");
                    var basicPending = await dbContext.Vehicles.CountAsync(v => (v.Status ?? "pending") == "pending");
                    var basicRejected = await dbContext.Vehicles.CountAsync(v => (v.Status ?? "pending") == "rejected");
                    
                    analytics = new
                    {
                        newVehiclesThisWeek = 0,
                        newVehiclesThisMonth = 0,
                        newVehiclesToday = 0,
                        approvedThisWeek = 0,
                        pendingVehicles = basicPending,
                        avgPrice = 0.0,
                        uniqueBrands = 0,
                        uniqueTypes = 0
                    };
                    statusDistribution = Array.Empty<object>();
                    brandDistribution = Array.Empty<object>();
                    typeDistribution = Array.Empty<object>();
                    priceRangeDistribution = Array.Empty<object>();
                    statistics = new
                    {
                        totalVehicles = basicTotal,
                        approvedVehicles = basicApproved,
                        pendingVehicles = basicPending,
                        rejectedVehicles = basicRejected,
                        avgPrice = 0.0,
                        minPrice = 0.0,
                        maxPrice = 0.0
                    };
                }
                catch
                {
                    // If even basic queries fail, return zeros
                analytics = new
                {
                    newVehiclesThisWeek = 0,
                    newVehiclesThisMonth = 0,
                    newVehiclesToday = 0,
                    approvedThisWeek = 0,
                    pendingVehicles = 0,
                    avgPrice = 0.0,
                    uniqueBrands = 0,
                    uniqueTypes = 0
                };
                statusDistribution = Array.Empty<object>();
                brandDistribution = Array.Empty<object>();
                typeDistribution = Array.Empty<object>();
                priceRangeDistribution = Array.Empty<object>();
                statistics = new
                {
                    totalVehicles = 0,
                    approvedVehicles = 0,
                    pendingVehicles = 0,
                    rejectedVehicles = 0,
                    avgPrice = 0.0,
                    minPrice = 0.0,
                    maxPrice = 0.0
                };
                }
            }

            return Ok(new
            {
                vehicles,
                recentActivities = recentActivitiesList,
                analytics,
                statusDistribution,
                brandDistribution,
                typeDistribution,
                priceRangeDistribution,
                statistics,
                pagination = export ? null : new
                {
                    page,
                    limit,
                    total,
                    totalPages = (int)Math.Ceiling((double)total / limit)
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Internal server error", 
                details = ex.Message,
                innerException = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    [HttpGet("payments")]
    public async Task<ActionResult> GetPayments(
        [FromServices] ApplicationDbContext dbContext,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string status = "all",
        [FromQuery] string? startDate = null,
        [FromQuery] string? endDate = null)
    {
        try
        {
            // Build query
            var query = dbContext.Subscriptions
                .Include(s => s.User)
                .AsQueryable();

            // Apply status filter
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                query = query.Where(s => s.Status == status);
            }

            // Apply date filters
            if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var start))
            {
                query = query.Where(s => s.CreatedAt >= start);
            }

            if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var end))
            {
                query = query.Where(s => s.CreatedAt <= end.AddDays(1)); // Include the entire end date
            }

            // Get total count before pagination
            var total = await query.CountAsync();

            // Apply pagination
            var subscriptions = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            // Load subscription plans for plan details
            // Load all plans first to avoid LINQ translation issues with Contains()
            var allPlans = await dbContext.SubscriptionPlans
                .AsNoTracking()
                .ToListAsync();
            
            var planDict = new Dictionary<string, SubscriptionPlan>();
            if (subscriptions.Any() && allPlans.Any())
            {
                var planTypes = subscriptions
                    .Select(s => s.PlanType)
                    .Where(pt => !string.IsNullOrEmpty(pt))
                    .Distinct()
                    .ToHashSet(); // Use HashSet for faster lookup
                
                var matchingPlans = allPlans
                    .Where(p => !string.IsNullOrEmpty(p.PlanType) && planTypes.Contains(p.PlanType))
                    .GroupBy(p => p.PlanType) // Group by PlanType to handle duplicates
                    .Select(g => g.First()) // Take first if duplicates exist
                    .ToList();
                
                planDict = matchingPlans.ToDictionary(p => p.PlanType, p => p);
            }

            // Map to DTOs with user and plan information
            var now = DateTime.UtcNow;
            var subscriptionDtos = subscriptions.Select(s =>
            {
                SubscriptionPlan? plan = null;
                if (!string.IsNullOrEmpty(s.PlanType) && planDict.ContainsKey(s.PlanType))
                {
                    plan = planDict[s.PlanType];
                }

                var isExpired = s.EndDate < now;
                var status = s.Status ?? "active";
                var displayStatus = status == "cancelled" ? "cancelled" :
                                   isExpired ? "expired" :
                                   status == "active" ? "active" : status;

                List<string> planFeatures = new();
                if (plan != null && !string.IsNullOrEmpty(plan.Features))
                {
                    try
                    {
                        planFeatures = System.Text.Json.JsonSerializer.Deserialize<List<string>>(plan.Features) ?? new List<string>();
                    }
                    catch
                    {
                        planFeatures = new List<string>();
                    }
                }

                return new
                {
                    id = s.Id ?? string.Empty,
                    userId = s.UserId.ToString(),
                    planType = s.PlanType ?? "unknown",
                    status = status,
                    startDate = s.StartDate.ToString("yyyy-MM-dd"),
                    endDate = s.EndDate.ToString("yyyy-MM-dd"),
                    price = s.Price,
                    paymentMethod = s.PaymentMethod ?? "N/A",
                    transactionId = s.TransactionId ?? "N/A",
                    createdAt = s.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    updatedAt = s.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    userName = s.User?.Name ?? "Unknown",
                    userEmail = s.User?.Email ?? "N/A",
                    userPhone = s.User?.Phone ?? "N/A",
                    planName = plan?.Name ?? (s.PlanType ?? "Unknown"),
                    planFeatures = planFeatures,
                    displayStatus = displayStatus
                };
            }).ToList();

            // Calculate statistics - load all subscriptions and calculate in memory
            var allSubscriptions = await dbContext.Subscriptions.ToListAsync();
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var todayStart = now.Date;

            // Calculate statistics with safe operations
            var activeOrCancelled = allSubscriptions
                .Where(s => 
                {
                    var status = s.Status ?? "active";
                    return status == "active" || status == "cancelled";
                })
                .ToList();

            var totalRevenue = 0.0;
            try
            {
                totalRevenue = activeOrCancelled.Sum(s => (double)s.Price);
            }
            catch { }

            var monthlyRevenue = 0.0;
            try
            {
                var monthlySubs = activeOrCancelled.Where(s => s.CreatedAt >= thisMonthStart).ToList();
                monthlyRevenue = monthlySubs.Sum(s => (double)s.Price);
            }
            catch { }

            var yearlyRevenue = 0.0;
            try
            {
                var yearlySubs = activeOrCancelled.Where(s => s.CreatedAt.Year == now.Year).ToList();
                yearlyRevenue = yearlySubs.Sum(s => (double)s.Price);
            }
            catch { }

            var averageSubscriptionValue = 0.0;
            try
            {
                if (allSubscriptions.Any())
                {
                    averageSubscriptionValue = allSubscriptions.Average(s => (double)s.Price);
                }
            }
            catch { }

            var stats = new
            {
                totalSubscriptions = allSubscriptions.Count,
                activeSubscriptions = allSubscriptions.Count(s => 
                {
                    var status = s.Status ?? "active";
                    return status == "active" && s.EndDate >= now;
                }),
                cancelledSubscriptions = allSubscriptions.Count(s => (s.Status ?? "active") == "cancelled"),
                expiredSubscriptions = allSubscriptions.Count(s => s.EndDate < now && (s.Status ?? "active") != "cancelled"),
                totalRevenue = totalRevenue,
                monthlyRevenue = monthlyRevenue,
                yearlyRevenue = yearlyRevenue,
                averageSubscriptionValue = averageSubscriptionValue,
                thisMonthSubscriptions = allSubscriptions.Count(s => s.CreatedAt >= thisMonthStart),
                todaySubscriptions = allSubscriptions.Count(s => s.CreatedAt >= todayStart),
                successfulPayments = activeOrCancelled.Count
            };

            var pagination = new
            {
                page,
                limit,
                total,
                totalPages = total > 0 ? (int)Math.Ceiling((double)total / limit) : 0
            };

            // Ensure all values are serializable
            var response = new
            {
                stats = stats,
                subscriptions = subscriptionDtos,
                pagination = pagination
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            // Log the full exception details for debugging
            Console.WriteLine($"Error in GetPayments: {ex.GetType().Name}");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"InnerException: {ex.InnerException.GetType().Name} - {ex.InnerException.Message}");
            }
            
            return StatusCode(500, new
            {
                error = "Failed to fetch payment data",
                details = ex.Message,
                exceptionType = ex.GetType().Name,
                innerException = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    [HttpGet("subscription-plans")]
    public async Task<ActionResult> GetSubscriptionPlans(
        [FromServices] ApplicationDbContext dbContext)
    {
        try
        {
            var plans = await dbContext.SubscriptionPlans
                .AsNoTracking()
                .OrderBy(p => p.PlanType)
                .ThenBy(p => p.Price)
                .ToListAsync();

            var planDtos = plans.Select(p =>
            {
                List<string> features = new();
                if (!string.IsNullOrEmpty(p.Features))
                {
                    try
                    {
                        features = System.Text.Json.JsonSerializer.Deserialize<List<string>>(p.Features) ?? new List<string>();
                    }
                    catch
                    {
                        features = new List<string>();
                    }
                }

                return new
                {
                    id = p.Id,
                    name = p.Name,
                    planType = p.PlanType,
                    price = p.Price,
                    postCount = p.PostCount,
                    features = features,
                    isActive = p.IsActive,
                    createdAt = p.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss")
                };
            }).ToList();

            return Ok(new { plans = planDtos });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "Failed to fetch subscription plans",
                details = ex.Message,
                innerException = ex.InnerException?.Message
            });
        }
    }

    [HttpPost("subscription-plans")]
    public async Task<ActionResult> CreateSubscriptionPlan(
        [FromServices] ApplicationDbContext dbContext,
        [FromBody] CreateSubscriptionPlanRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { error = "Plan name is required" });
            }

            if (string.IsNullOrWhiteSpace(request.PlanType))
            {
                return BadRequest(new { error = "Plan type is required" });
            }

            if (request.Price < 0)
            {
                return BadRequest(new { error = "Price must be greater than or equal to 0" });
            }

            if (request.PostCount < 0)
            {
                return BadRequest(new { error = "Post count must be greater than or equal to 0" });
            }

            // Check if plan with same name and type already exists
            var existingPlan = await dbContext.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Name == request.Name && p.PlanType == request.PlanType);

            if (existingPlan != null)
            {
                return Conflict(new { error = "A plan with this name and type already exists" });
            }

            // Create new plan
            var plan = new SubscriptionPlan
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name.Trim(),
                PlanType = request.PlanType.Trim().ToLower(),
                Price = request.Price,
                PostCount = request.PostCount,
                Features = System.Text.Json.JsonSerializer.Serialize(request.Features ?? new List<string>()),
                IsActive = request.IsActive ?? true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.SubscriptionPlans.Add(plan);
            await dbContext.SaveChangesAsync();

            // Return created plan
            List<string> features = new();
            if (!string.IsNullOrEmpty(plan.Features))
            {
                try
                {
                    features = System.Text.Json.JsonSerializer.Deserialize<List<string>>(plan.Features) ?? new List<string>();
                }
                catch
                {
                    features = new List<string>();
                }
            }

            var planDto = new
            {
                id = plan.Id,
                name = plan.Name,
                planType = plan.PlanType,
                price = plan.Price,
                postCount = plan.PostCount,
                features = features,
                isActive = plan.IsActive,
                createdAt = plan.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss")
            };

            return StatusCode(201, new { 
                message = "Subscription plan created successfully",
                plan = planDto
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "Failed to create subscription plan",
                details = ex.Message,
                innerException = ex.InnerException?.Message
            });
        }
    }

    [HttpPatch("subscription-plans/{planId}")]
    public async Task<ActionResult> UpdateSubscriptionPlan(
        string planId,
        [FromServices] ApplicationDbContext dbContext,
        [FromBody] UpdateSubscriptionPlanRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(planId))
            {
                return BadRequest(new { error = "Plan ID is required" });
            }

            var plan = await dbContext.SubscriptionPlans.FindAsync(planId);
            if (plan == null)
            {
                return NotFound(new { error = "Subscription plan not found" });
            }

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                plan.Name = request.Name.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.PlanType))
            {
                plan.PlanType = request.PlanType.Trim().ToLower();
            }

            if (request.Price.HasValue)
            {
                if (request.Price.Value < 0)
                {
                    return BadRequest(new { error = "Price must be greater than or equal to 0" });
                }
                plan.Price = request.Price.Value;
            }

            if (request.PostCount.HasValue)
            {
                if (request.PostCount.Value < 0)
                {
                    return BadRequest(new { error = "Post count must be greater than or equal to 0" });
                }
                plan.PostCount = request.PostCount.Value;
            }

            if (request.Features != null)
            {
                plan.Features = System.Text.Json.JsonSerializer.Serialize(request.Features);
            }

            if (request.IsActive.HasValue)
            {
                plan.IsActive = request.IsActive.Value;
            }

            plan.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

            // Return updated plan
            List<string> features = new();
            if (!string.IsNullOrEmpty(plan.Features))
            {
                try
                {
                    features = System.Text.Json.JsonSerializer.Deserialize<List<string>>(plan.Features) ?? new List<string>();
                }
                catch
                {
                    features = new List<string>();
                }
            }

            var planDto = new
            {
                id = plan.Id,
                name = plan.Name,
                planType = plan.PlanType,
                price = plan.Price,
                postCount = plan.PostCount,
                features = features,
                isActive = plan.IsActive,
                createdAt = plan.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss")
            };

            return Ok(new { 
                message = "Subscription plan updated successfully",
                plan = planDto
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "Failed to update subscription plan",
                details = ex.Message,
                innerException = ex.InnerException?.Message
            });
        }
    }

    [HttpDelete("subscription-plans/{planId}")]
    public async Task<ActionResult> DeleteSubscriptionPlan(
        string planId,
        [FromServices] ApplicationDbContext dbContext)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(planId))
            {
                return BadRequest(new { error = "Plan ID is required" });
            }

            var plan = await dbContext.SubscriptionPlans.FindAsync(planId);
            if (plan == null)
            {
                return NotFound(new { error = "Subscription plan not found" });
            }

            // Check if plan is being used by any active subscriptions
            var activeSubscriptions = await dbContext.Subscriptions
                .Where(s => s.PlanType == plan.PlanType && 
                           (s.Status == "active" || s.Status == null) &&
                           s.EndDate > DateTime.UtcNow)
                .CountAsync();

            if (activeSubscriptions > 0)
            {
                return BadRequest(new { 
                    error = $"Cannot delete plan. There are {activeSubscriptions} active subscription(s) using this plan type." 
                });
            }

            dbContext.SubscriptionPlans.Remove(plan);
            await dbContext.SaveChangesAsync();

            return Ok(new { message = "Subscription plan deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "Failed to delete subscription plan",
                details = ex.Message,
                innerException = ex.InnerException?.Message
            });
        }
    }

    [HttpGet("subscriptions")]
    public async Task<ActionResult> GetSubscriptions(
        [FromServices] ApplicationDbContext dbContext)
    {
        try
        {
            var subscriptions = await dbContext.Subscriptions
                .Include(s => s.User)
                .AsNoTracking()
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var subscriptionDtos = subscriptions.Select(s =>
            {
                var isExpired = s.EndDate < now;
                var status = s.Status ?? "active";
                var displayStatus = status == "cancelled" ? "cancelled" :
                                   isExpired ? "expired" :
                                   status == "active" ? "active" : "pending";

                return new
                {
                    id = s.Id,
                    userId = s.UserId.ToString(),
                    planType = s.PlanType ?? "unknown",
                    status = displayStatus,
                    startDate = s.StartDate.ToString("yyyy-MM-dd"),
                    endDate = s.EndDate.ToString("yyyy-MM-dd"),
                    price = s.Price,
                    paymentMethod = s.PaymentMethod ?? "N/A",
                    transactionId = s.TransactionId ?? "N/A",
                    createdAt = s.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    updatedAt = s.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    userName = s.User?.Name ?? "Unknown",
                    userEmail = s.User?.Email ?? "N/A"
                };
            }).ToList();

            return Ok(new { subscriptions = subscriptionDtos });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "Failed to fetch subscriptions",
                details = ex.Message,
                innerException = ex.InnerException?.Message
            });
        }
    }

    [HttpGet("subscription-stats")]
    public async Task<ActionResult> GetSubscriptionStats(
        [FromServices] ApplicationDbContext dbContext)
    {
        try
        {
            var allSubscriptions = await dbContext.Subscriptions.ToListAsync();
            var now = DateTime.UtcNow;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var thisYearStart = new DateTime(now.Year, 1, 1);

            var activeOrCancelled = allSubscriptions
                .Where(s =>
                {
                    var status = s.Status ?? "active";
                    return status == "active" || status == "cancelled";
                })
                .ToList();

            var totalRevenue = 0.0;
            try
            {
                totalRevenue = activeOrCancelled.Sum(s => (double)s.Price);
            }
            catch { }

            var monthlyRevenue = 0.0;
            try
            {
                var monthlySubs = activeOrCancelled.Where(s => s.CreatedAt >= thisMonthStart).ToList();
                monthlyRevenue = monthlySubs.Sum(s => (double)s.Price);
            }
            catch { }

            var yearlyRevenue = 0.0;
            try
            {
                var yearlySubs = activeOrCancelled.Where(s => s.CreatedAt >= thisYearStart).ToList();
                yearlyRevenue = yearlySubs.Sum(s => (double)s.Price);
            }
            catch { }

            var averageSubscriptionValue = 0.0;
            try
            {
                if (allSubscriptions.Any())
                {
                    averageSubscriptionValue = allSubscriptions.Average(s => (double)s.Price);
                }
            }
            catch { }

            var stats = new
            {
                totalSubscriptions = allSubscriptions.Count,
                activeSubscriptions = allSubscriptions.Count(s => (s.Status ?? "active") == "active" && s.EndDate >= now),
                cancelledSubscriptions = allSubscriptions.Count(s => (s.Status ?? "active") == "cancelled"),
                expiredSubscriptions = allSubscriptions.Count(s => s.EndDate < now && (s.Status ?? "active") != "cancelled"),
                pendingSubscriptions = allSubscriptions.Count(s => (s.Status ?? "active") == "pending"),
                totalRevenue = totalRevenue,
                monthlyRevenue = monthlyRevenue,
                yearlyRevenue = yearlyRevenue,
                averageSubscriptionValue = averageSubscriptionValue
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "Failed to fetch subscription stats",
                details = ex.Message,
                innerException = ex.InnerException?.Message
            });
        }
    }

    [HttpGet("settings")]
    public async Task<ActionResult> GetSettings(
        [FromServices] ApplicationDbContext dbContext)
    {
        try
        {
            // Get feature settings from database
            var settings = await dbContext.Settings
                .Where(s => s.SettingKey.StartsWith("feature_"))
                .ToDictionaryAsync(s => s.SettingKey, s => s.Value);

            // Helper function to get boolean setting
            bool GetBoolSetting(string key, bool defaultValue)
            {
                if (settings.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
                {
                    return bool.TryParse(value, out var result) && result;
                }
                return defaultValue;
            }

            // Helper function to get string setting
            string GetStringSetting(string key, string defaultValue)
            {
                if (settings.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
                {
                    return value;
                }
                return defaultValue;
            }

            var features = new
            {
                userRegistration = GetBoolSetting("feature_userRegistration", true),
                pricePrediction = GetBoolSetting("feature_pricePrediction", true),
                proPlanActivation = GetBoolSetting("feature_proPlanActivation", true),
                maintenanceMode = GetBoolSetting("feature_maintenanceMode", false),
                maintenanceMessage = GetStringSetting("feature_maintenanceMessage", "We are currently performing scheduled maintenance. Please check back later.")
            };

            return Ok(new { features });
        }
        catch (Exception ex)
        {
            // Return default values if there's an error
            return Ok(new
            {
                features = new
                {
                    userRegistration = true,
                    pricePrediction = true,
                    proPlanActivation = true,
                    maintenanceMode = false,
                    maintenanceMessage = "We are currently performing scheduled maintenance. Please check back later."
                }
            });
        }
    }

    [HttpPut("settings")]
    public async Task<ActionResult> UpdateSettings(
        [FromServices] ApplicationDbContext dbContext,
        [FromBody] UpdateSettingsRequest request)
    {
        try
        {
            if (request?.Features == null)
            {
                return BadRequest(new { error = "Features are required" });
            }

            // Update or create feature settings
            var settingsToUpdate = new Dictionary<string, string>
            {
                { "feature_userRegistration", request.Features.UserRegistration.ToString() },
                { "feature_pricePrediction", request.Features.PricePrediction.ToString() },
                { "feature_proPlanActivation", request.Features.ProPlanActivation.ToString() },
                { "feature_maintenanceMode", request.Features.MaintenanceMode.ToString() },
                { "feature_maintenanceMessage", request.Features.MaintenanceMessage ?? "We are currently performing scheduled maintenance. Please check back later." }
            };

            foreach (var kvp in settingsToUpdate)
            {
                var setting = await dbContext.Settings
                    .FirstOrDefaultAsync(s => s.SettingKey == kvp.Key);

                if (setting != null)
                {
                    setting.Value = kvp.Value;
                    setting.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    setting = new Models.Setting
                    {
                        SettingKey = kvp.Key,
                        Value = kvp.Value,
                        UpdatedAt = DateTime.UtcNow
                    };
                    dbContext.Settings.Add(setting);
                }
            }

            await dbContext.SaveChangesAsync();

            return Ok(new { message = "Settings updated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                error = "Failed to update settings",
                details = ex.Message,
                innerException = ex.InnerException?.Message
            });
        }
    }

    [HttpGet("permissions")]
    [Authorize(Roles = "superadmin")]
    public async Task<ActionResult> GetAllPermissions([FromServices] ApplicationDbContext dbContext)
    {
        try
        {
            // Get all admin users (admin and superadmin roles)
            var admins = await dbContext.Users
                .Where(u => u.Role == "admin" || u.Role == "superadmin")
                .ToListAsync();

            // Get all permissions
            var permissions = await dbContext.AdminPermissions
                .Include(p => p.Admin)
                .ToListAsync();

            // Build response with permissions for each admin
            var permissionDtos = permissions.Select(p => new
            {
                id = p.Id,
                adminId = p.AdminId.ToString(),
                feature = p.Feature,
                canAccess = p.CanAccess,
                canCreate = p.CanCreate,
                canEdit = p.CanEdit,
                canDelete = p.CanDelete,
                adminName = p.Admin?.Name ?? "Unknown",
                adminEmail = p.Admin?.Email ?? "Unknown",
                adminRole = p.Admin?.Role ?? "admin"
            }).ToList();

            // Also include admins that don't have any permissions yet
            var adminIdsWithPermissions = permissions.Select(p => p.AdminId).Distinct().ToHashSet();
            var adminsWithoutPermissions = admins.Where(a => !adminIdsWithPermissions.Contains(a.Id)).ToList();

            // For each admin without permissions, create default permission entries for all features
            var availableFeatures = new[] { "user_management", "vehicle_management", "settings_management", "payment_management" };
            
            foreach (var admin in adminsWithoutPermissions)
            {
                // Only create permissions for regular admins, not superadmins
                if (admin.Role == "admin")
                {
                    foreach (var feature in availableFeatures)
                    {
                        permissionDtos.Add(new
                        {
                            id = 0, // Will be set when created
                            adminId = admin.Id.ToString(),
                            feature = feature,
                            canAccess = false,
                            canCreate = false,
                            canEdit = false,
                            canDelete = false,
                            adminName = admin.Name ?? "Unknown",
                            adminEmail = admin.Email ?? "Unknown",
                            adminRole = admin.Role ?? "admin"
                        });
                    }
                }
            }

            return Ok(new { permissions = permissionDtos });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Internal server error", 
                details = ex.Message,
                innerException = ex.InnerException?.Message
            });
        }
    }

    [HttpGet("permissions/user")]
    public async Task<ActionResult> GetUserPermissions([FromServices] ApplicationDbContext dbContext)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { error = "Authentication required" });
            }

            if (!Guid.TryParse(currentUserId, out var userIdGuid))
            {
                return BadRequest(new { error = "Invalid user ID" });
            }

            // Get user's role
            var user = await dbContext.Users.FindAsync(userIdGuid);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            // Super admins have all permissions (frontend handles this, but return empty array)
            if (user.Role == "superadmin")
            {
                return Ok(new { permissions = Array.Empty<object>() });
            }

            // Regular users have no admin permissions
            if (user.Role != "admin")
            {
                return Ok(new { permissions = Array.Empty<object>() });
            }

            // Get admin permissions
            var permissions = await dbContext.AdminPermissions
                .Where(p => p.AdminId == userIdGuid)
                .Select(p => new
                {
                    feature = p.Feature,
                    canAccess = p.CanAccess,
                    canCreate = p.CanCreate,
                    canEdit = p.CanEdit,
                    canDelete = p.CanDelete
                })
                .ToListAsync();

            return Ok(new { permissions });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Internal server error", 
                details = ex.Message,
                innerException = ex.InnerException?.Message
            });
        }
    }

    [HttpPost("permissions")]
    [Authorize(Roles = "superadmin")]
    public async Task<ActionResult> UpdatePermission(
        [FromServices] ApplicationDbContext dbContext,
        [FromBody] UpdatePermissionRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.AdminId))
            {
                return BadRequest(new { error = "Admin ID is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Feature))
            {
                return BadRequest(new { error = "Feature is required" });
            }

            if (request.Permissions == null)
            {
                return BadRequest(new { error = "Permissions are required" });
            }

            if (!Guid.TryParse(request.AdminId, out var adminIdGuid))
            {
                return BadRequest(new { error = "Invalid admin ID" });
            }

            // Verify the admin exists and is not a superadmin
            var admin = await dbContext.Users.FindAsync(adminIdGuid);
            if (admin == null)
            {
                return NotFound(new { error = "Admin not found" });
            }

            if (admin.Role == "superadmin")
            {
                return BadRequest(new { error = "Cannot modify permissions for super admins" });
            }

            // Find or create permission
            var permission = await dbContext.AdminPermissions
                .FirstOrDefaultAsync(p => p.AdminId == adminIdGuid && p.Feature == request.Feature);

            if (permission == null)
            {
                permission = new Models.AdminPermission
                {
                    AdminId = adminIdGuid,
                    Feature = request.Feature,
                    CanAccess = request.Permissions.CanAccess,
                    CanCreate = request.Permissions.CanCreate,
                    CanEdit = request.Permissions.CanEdit,
                    CanDelete = request.Permissions.CanDelete
                };
                dbContext.AdminPermissions.Add(permission);
            }
            else
            {
                permission.CanAccess = request.Permissions.CanAccess;
                permission.CanCreate = request.Permissions.CanCreate;
                permission.CanEdit = request.Permissions.CanEdit;
                permission.CanDelete = request.Permissions.CanDelete;
            }

            await dbContext.SaveChangesAsync();

            // Return updated permission
            var permissionDto = new
            {
                id = permission.Id,
                adminId = permission.AdminId.ToString(),
                feature = permission.Feature,
                canAccess = permission.CanAccess,
                canCreate = permission.CanCreate,
                canEdit = permission.CanEdit,
                canDelete = permission.CanDelete,
                adminName = admin.Name ?? "Unknown",
                adminEmail = admin.Email ?? "Unknown",
                adminRole = admin.Role ?? "admin"
            };

            return Ok(new { 
                message = "Permission updated successfully",
                permission = permissionDto
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = "Internal server error", 
                details = ex.Message,
                innerException = ex.InnerException?.Message,
                stackTrace = ex.StackTrace
            });
        }
    }
}

// DTOs for settings
public class UpdateSettingsRequest
{
    public FeatureSettingsDto? Features { get; set; }
}

public class FeatureSettingsDto
{
    public bool UserRegistration { get; set; }
    public bool PricePrediction { get; set; }
    public bool ProPlanActivation { get; set; }
    public bool MaintenanceMode { get; set; }
    public string? MaintenanceMessage { get; set; }
}

// DTOs for admin management
public class CreateAdminRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "admin";
    public string? Phone { get; set; }
}

public class ToggleAdminBlockRequest
{
    public string AdminId { get; set; } = string.Empty;
    public bool Block { get; set; }
}

public class DeleteAdminRequest
{
    public string AdminId { get; set; } = string.Empty;
}

// DTOs for permissions
public class UpdatePermissionRequest
{
    public string AdminId { get; set; } = string.Empty;
    public string Feature { get; set; } = string.Empty;
    public PermissionData? Permissions { get; set; }
}

public class PermissionData
{
    public bool CanAccess { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}


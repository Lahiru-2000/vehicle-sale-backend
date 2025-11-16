using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VehiclePricePrediction.API.DTOs;
using VehiclePricePrediction.API.Services;

namespace VehiclePricePrediction.API.Controllers;

[ApiController]
[Route("api/favorites")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IFavoriteService _favoriteService;

    public FavoritesController(IFavoriteService favoriteService)
    {
        _favoriteService = favoriteService;
    }

    [HttpGet]
    public async Task<ActionResult<List<VehicleDto>>> GetFavorites()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        try
        {
            var favorites = await _favoriteService.GetUserFavoritesAsync(userId);
            return Ok(new { favorites });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult> AddFavorite([FromBody] AddFavoriteRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        if (!int.TryParse(request.VehicleId, out var vehicleId))
        {
            return BadRequest(new { error = "Invalid vehicle ID" });
        }

        try
        {
            await _favoriteService.AddFavoriteAsync(userId, vehicleId);
            return Ok(new { message = "Vehicle added to favorites" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<ActionResult> RemoveFavorite([FromQuery] string vehicleId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        if (!int.TryParse(vehicleId, out var vehicleIdInt))
        {
            return BadRequest(new { error = "Invalid vehicle ID" });
        }

        try
        {
            await _favoriteService.RemoveFavoriteAsync(userId, vehicleIdInt);
            return Ok(new { message = "Vehicle removed from favorites" });
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

    [HttpGet("check")]
    public async Task<ActionResult<CheckFavoriteResponse>> CheckFavorite([FromQuery] string vehicleId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        if (!int.TryParse(vehicleId, out var vehicleIdInt))
        {
            return BadRequest(new { error = "Invalid vehicle ID" });
        }

        try
        {
            var isFavorite = await _favoriteService.IsFavoriteAsync(userId, vehicleIdInt);
            return Ok(new CheckFavoriteResponse { IsFavorite = isFavorite });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }
}

public class AddFavoriteRequest
{
    public string VehicleId { get; set; } = string.Empty;
}

public class CheckFavoriteResponse
{
    public bool IsFavorite { get; set; }
}


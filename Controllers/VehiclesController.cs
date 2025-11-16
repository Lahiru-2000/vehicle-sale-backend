using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VehiclePricePrediction.API.DTOs;
using VehiclePricePrediction.API.Services;

namespace VehiclePricePrediction.API.Controllers;

[ApiController]
[Route("api/vehicles")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;
    private readonly IConfiguration _configuration;

    public VehiclesController(IVehicleService vehicleService, IConfiguration configuration)
    {
        _vehicleService = vehicleService;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<VehicleListResponse>> GetVehicles([FromQuery] VehicleQueryParams queryParams)
    {
        string? userId = null;
        if (queryParams.MyPosts)
        {
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "Authentication required to view your posts" });
            }
        }

        var response = await _vehicleService.GetVehiclesAsync(queryParams, userId);
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<VehicleDto>> GetVehicle(string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var vehicle = await _vehicleService.GetVehicleByIdAsync(id, userId);
        
        if (vehicle == null)
        {
            return NotFound(new { error = "Vehicle not found" });
        }

        return Ok(new { vehicle });
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<VehicleDto>> CreateVehicle([FromBody] CreateVehicleRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "Authentication required to create vehicles" });
        }

        try
        {
            var vehicle = await _vehicleService.CreateVehicleAsync(request, userId);
            return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.Id }, vehicle);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    [Authorize]
    [HttpPut]
    public async Task<ActionResult<VehicleDto>> UpdateVehicle([FromBody] UpdateVehicleRequest request, [FromQuery] string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "Authentication required to update vehicles" });
        }

        if (string.IsNullOrEmpty(id))
        {
            return BadRequest(new { error = "Vehicle ID is required" });
        }

        try
        {
            var vehicle = await _vehicleService.UpdateVehicleAsync(id, request, userId);
            return Ok(new { message = "Vehicle updated successfully", vehicle });
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
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [Authorize]
    [HttpDelete]
    public async Task<ActionResult> DeleteVehicle([FromQuery] string id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "Authentication required to delete vehicles" });
        }

        if (string.IsNullOrEmpty(id))
        {
            return BadRequest(new { error = "Vehicle ID is required" });
        }

        try
        {
            await _vehicleService.DeleteVehicleAsync(id, userId);
            return Ok(new { message = "Vehicle deleted successfully" });
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
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPost("predict-price")]
    public async Task<ActionResult<PricePredictionResponse>> PredictPrice([FromBody] PricePredictionRequest request)
    {
        var response = await _vehicleService.PredictPriceAsync(request, _configuration);
        
        if (!response.Success)
        {
            if (response.Error == "Vehicle not found")
            {
                return NotFound(response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }
}


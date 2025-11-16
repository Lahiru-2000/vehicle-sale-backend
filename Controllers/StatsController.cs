using Microsoft.AspNetCore.Mvc;
using VehiclePricePrediction.API.Services;

namespace VehiclePricePrediction.API.Controllers;

[ApiController]
[Route("api/stats")]
public class StatsController : ControllerBase
{
    private readonly IStatsService _statsService;

    public StatsController(IStatsService statsService)
    {
        _statsService = statsService;
    }

    [HttpGet("users")]
    public async Task<ActionResult> GetActiveUsersCount()
    {
        try
        {
            var count = await _statsService.GetActiveUsersCountAsync();
            return Ok(new { activeUsersCount = count, success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }
}


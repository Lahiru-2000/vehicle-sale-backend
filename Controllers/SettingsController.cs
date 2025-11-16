using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePricePrediction.API.Data;

namespace VehiclePricePrediction.API.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SettingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("features")]
    public async Task<ActionResult> GetFeatureSettings()
    {
        try
        {
            // Get feature settings from database
            var settings = await _context.Settings
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

            // Feature settings matching frontend expectations
            var features = new
            {
                userRegistration = GetBoolSetting("feature_userRegistration", true),
                pricePrediction = GetBoolSetting("feature_pricePrediction", true),
                proPlanActivation = GetBoolSetting("feature_proPlanActivation", true),
                maintenanceMode = GetBoolSetting("feature_maintenanceMode", false),
                maintenanceMessage = GetStringSetting("feature_maintenanceMessage", "We are currently performing scheduled maintenance. Please check back later.")
            };

            return Ok(features);
        }
        catch (Exception ex)
        {
            // Return default values if there's an error
            return Ok(new
            {
                userRegistration = true,
                pricePrediction = true,
                proPlanActivation = true,
                maintenanceMode = false,
                maintenanceMessage = "We are currently performing scheduled maintenance. Please check back later."
            });
        }
    }
}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VehiclePricePrediction.API.Controllers;

[ApiController]
[Route("api/upload")]
[Authorize]
public class UploadController : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    [HttpPost("images")]
    public async Task<ActionResult> UploadImages([FromForm] List<IFormFile> images)
    {
        try
        {
            if (images == null || images.Count == 0)
            {
                return BadRequest(new { error = "No images provided" });
            }

            if (images.Count > 8)
            {
                return BadRequest(new { error = "Maximum 8 images allowed" });
            }

            var uploadedImages = new List<string>();

            foreach (var file in images)
            {
                // Validate file size
                if (file.Length > MaxFileSize)
                {
                    return BadRequest(new { error = $"File {file.FileName} exceeds maximum size of 10MB" });
                }

                // Validate file type
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
                {
                    return BadRequest(new { error = $"File {file.FileName} is not a valid image type. Allowed types: jpg, jpeg, png, gif, webp" });
                }

                // Validate content type
                if (!file.ContentType.StartsWith("image/"))
                {
                    return BadRequest(new { error = $"File {file.FileName} is not a valid image file" });
                }

                // Convert to base64
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();
                    var base64String = Convert.ToBase64String(fileBytes);
                    var dataUrl = $"data:{file.ContentType};base64,{base64String}";
                    uploadedImages.Add(dataUrl);
                }
            }

            return Ok(new { images = uploadedImages });
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


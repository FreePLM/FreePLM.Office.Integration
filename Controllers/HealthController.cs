using Microsoft.AspNetCore.Mvc;

namespace FreePLM.Office.Integration.Controllers;

/// <summary>
/// Health check controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogDebug("Health check requested");

        return Ok(new
        {
            status = "Healthy",
            service = "FreePLM.Office.Integration",
            version = "1.0.0",
            timestamp = DateTime.UtcNow
        });
    }
}

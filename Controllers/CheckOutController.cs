using Microsoft.AspNetCore.Mvc;
using FreePLM.Office.Integration.Models;
using FreePLM.Database.Services;

namespace FreePLM.Office.Integration.Controllers;

/// <summary>
/// Controller for CheckOut/CheckIn operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CheckOutController : ControllerBase
{
    private readonly ILogger<CheckOutController> _logger;
    private readonly ICheckOutService _checkOutService;

    public CheckOutController(ILogger<CheckOutController> logger, ICheckOutService checkOutService)
    {
        _logger = logger;
        _checkOutService = checkOutService;
    }

    /// <summary>
    /// Check out a document
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CheckOut([FromBody] CheckOutRequest request)
    {
        _logger.LogInformation("CheckOut request for {ObjectId}", request.ObjectId);

        // TODO: Get actual user ID from authentication
        var userId = "user@example.com";

        var result = await _checkOutService.CheckOutAsync(
            request.ObjectId,
            userId,
            request.MachineName,
            request.Comment);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new
        {
            success = result.Success,
            objectId = result.ObjectId,
            revision = result.Revision,
            downloadUrl = $"http://localhost:5000/api/documents/{result.ObjectId}/content",
            checkedOutDate = result.CheckedOutDate,
            message = result.Message
        });
    }

    /// <summary>
    /// Check in a document
    /// </summary>
    [HttpPost("checkin")]
    public async Task<IActionResult> CheckIn([FromForm] CheckInRequest request)
    {
        _logger.LogInformation("CheckIn request for {ObjectId}, major={Major}", request.ObjectId, request.CreateMajorRevision);

        // TODO: Get actual user ID from authentication
        var userId = "user@example.com";

        if (request.File == null)
        {
            return BadRequest(new { message = "File is required" });
        }

        using var fileStream = request.File.OpenReadStream();

        var result = await _checkOutService.CheckInAsync(
            request.ObjectId,
            fileStream,
            userId,
            request.Comment,
            request.CreateMajorRevision,
            request.NewStatus);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new
        {
            success = result.Success,
            objectId = result.ObjectId,
            newRevision = result.NewRevision,
            previousRevision = result.PreviousRevision,
            checkedInDate = result.CheckedInDate,
            message = result.Message
        });
    }

    /// <summary>
    /// Cancel checkout
    /// </summary>
    [HttpPost("cancel")]
    public async Task<IActionResult> CancelCheckOut([FromBody] CancelCheckOutRequest request)
    {
        _logger.LogInformation("Cancel CheckOut for {ObjectId}", request.ObjectId);

        // TODO: Get actual user ID from authentication
        var userId = "user@example.com";

        var success = await _checkOutService.CancelCheckOutAsync(request.ObjectId, userId);

        if (!success)
        {
            return BadRequest(new { message = "Failed to cancel checkout. Document may not be checked out by you." });
        }

        return Ok(new
        {
            success = true,
            message = "Checkout cancelled successfully"
        });
    }

    /// <summary>
    /// Get checkout status
    /// </summary>
    [HttpGet("{objectId}/status")]
    public async Task<IActionResult> GetCheckOutStatus(string objectId)
    {
        _logger.LogInformation("Get checkout status for {ObjectId}", objectId);

        var lockStatus = await _checkOutService.GetCheckOutStatusAsync(objectId);

        return Ok(new
        {
            isLocked = lockStatus != null,
            lockedBy = lockStatus?.LockedBy,
            lockedDate = lockStatus?.LockedDate,
            workingRevision = lockStatus?.WorkingRevision
        });
    }
}

public record CheckOutRequest(string ObjectId, string? Comment, string? MachineName);
public record CancelCheckOutRequest(string ObjectId);

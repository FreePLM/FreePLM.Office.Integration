using System.IO;
using Microsoft.AspNetCore.Mvc;
using FreePLM.Office.Integration.Models;
using FreePLM.Office.Integration.Services;
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
    private readonly IDialogService _dialogService;

    public CheckOutController(
        ILogger<CheckOutController> logger,
        ICheckOutService checkOutService,
        IDialogService dialogService)
    {
        _logger = logger;
        _checkOutService = checkOutService;
        _dialogService = dialogService;
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

    /// <summary>
    /// Show check-in UI dialog and process the check-in
    /// </summary>
    [HttpPost("checkin-ui")]
    public async Task<IActionResult> CheckInWithUI([FromBody] CheckInUIRequest request)
    {
        _logger.LogInformation("Showing CheckIn UI for {ObjectId}", request.ObjectId);

        try
        {
            // Show the WPF dialog
            var dialogResult = await _dialogService.ShowCheckInDialogAsync(
                request.ObjectId,
                request.FileName,
                request.CurrentRevision);

            if (dialogResult == null || !dialogResult.Success)
            {
                return Ok(new { success = false, message = "Check-in cancelled by user" });
            }

            // Process the check-in
            var userId = "user@example.com"; // TODO: Get from authentication

            using var fileStream = new MemoryStream(dialogResult.FileContent!);

            var result = await _checkOutService.CheckInAsync(
                dialogResult.ObjectId,
                fileStream,
                userId,
                dialogResult.Comment,
                dialogResult.CreateMajorRevision,
                null);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during check-in with UI");
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
    }
}

public record CheckOutRequest(string ObjectId, string? Comment, string? MachineName);
public record CancelCheckOutRequest(string ObjectId);
public record CheckInUIRequest(string ObjectId, string FileName, string CurrentRevision);

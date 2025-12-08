using Microsoft.AspNetCore.Mvc;
using FreePLM.Database.Services;

namespace FreePLM.Office.Integration.Controllers;

/// <summary>
/// Controller for workflow and status management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
    private readonly ILogger<WorkflowController> _logger;
    private readonly IWorkflowService _workflowService;
    private readonly IDocumentService _documentService;

    public WorkflowController(
        ILogger<WorkflowController> logger,
        IWorkflowService workflowService,
        IDocumentService documentService)
    {
        _logger = logger;
        _workflowService = workflowService;
        _documentService = documentService;
    }

    /// <summary>
    /// Change document status
    /// </summary>
    [HttpPost("status")]
    public async Task<IActionResult> ChangeStatus([FromBody] StatusChangeRequest request)
    {
        _logger.LogInformation("Change status for {ObjectId} to {NewStatus}",
            request.ObjectId, request.NewStatus);

        // TODO: Get actual user ID from authentication
        var userId = "user@example.com";

        // Get current document to know old status
        var document = await _documentService.GetDocumentAsync(request.ObjectId);
        if (document == null)
        {
            return NotFound(new { message = "Document not found" });
        }

        var oldStatus = document.Status;

        var success = await _workflowService.ChangeStatusAsync(
            request.ObjectId,
            request.NewStatus,
            userId,
            request.Comment);

        if (!success)
        {
            return BadRequest(new { message = "Invalid status transition" });
        }

        return Ok(new
        {
            success = true,
            objectId = request.ObjectId,
            oldStatus = oldStatus,
            newStatus = request.NewStatus,
            message = "Status changed successfully"
        });
    }

    /// <summary>
    /// Get status history
    /// </summary>
    [HttpGet("{objectId}/history")]
    public async Task<IActionResult> GetStatusHistory(string objectId)
    {
        _logger.LogInformation("Get status history for {ObjectId}", objectId);

        var history = await _workflowService.GetWorkflowHistoryAsync(objectId);

        return Ok(history);
    }
}

public record StatusChangeRequest(string ObjectId, string NewStatus, string Comment);

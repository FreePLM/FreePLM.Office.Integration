using Microsoft.AspNetCore.Mvc;

namespace FreePLM.Office.Integration.Controllers;

/// <summary>
/// Controller for workflow and status management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(ILogger<WorkflowController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Change document status
    /// </summary>
    [HttpPost("status")]
    public async Task<IActionResult> ChangeStatus([FromBody] StatusChangeRequest request)
    {
        _logger.LogInformation("Change status for {ObjectId} to {NewStatus}",
            request.ObjectId, request.NewStatus);

        // TODO: Implement with IWorkflowService
        return Ok(new
        {
            success = true,
            objectId = request.ObjectId,
            oldStatus = "InWork",
            newStatus = request.NewStatus,
            message = "Status changed successfully (mock)"
        });
    }

    /// <summary>
    /// Get status history
    /// </summary>
    [HttpGet("{objectId}/history")]
    public async Task<IActionResult> GetStatusHistory(string objectId)
    {
        _logger.LogInformation("Get status history for {ObjectId}", objectId);

        // TODO: Implement with IWorkflowService
        return Ok(new[]
        {
            new
            {
                historyId = 1,
                objectId = objectId,
                oldStatus = "Private",
                newStatus = "InWork",
                changedBy = "user@example.com",
                changedDate = DateTime.UtcNow.AddDays(-5),
                comment = "Starting work"
            }
        });
    }
}

public record StatusChangeRequest(string ObjectId, string NewStatus, string Comment);

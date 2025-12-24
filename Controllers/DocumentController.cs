using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using FreePLM.Database.Services;
using FreePLM.Office.Integration.Services;

namespace FreePLM.Office.Integration.Controllers;

/// <summary>
/// Controller for document operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly ILogger<DocumentController> _logger;
    private readonly IDocumentService _documentService;
    private readonly ICheckOutService _checkOutService;
    private readonly IDialogService _dialogService;

    public DocumentController(
        ILogger<DocumentController> logger,
        IDocumentService documentService,
        ICheckOutService checkOutService,
        IDialogService dialogService)
    {
        _logger = logger;
        _documentService = documentService;
        _checkOutService = checkOutService;
        _dialogService = dialogService;
    }

    /// <summary>
    /// Create a new document (creates initial version and checks it out)
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> CreateDocument([FromBody] CreateDocumentJsonRequest request)
    {
        _logger.LogInformation("Creating new document: {FileName}", request.FileName);

        try
        {
            // Create empty document content initially
            using var emptyStream = new MemoryStream();

            // Create the document in the database
            var objectId = await _documentService.CreateDocumentAsync(
                emptyStream,
                request.FileName,
                request.Owner ?? "user@example.com",
                request.Group ?? "Default",
                request.Role ?? "Default",
                request.Project ?? "Default",
                request.Comment);

            _logger.LogInformation("Document created: {ObjectId}", objectId);

            // Check out the document immediately so user can edit
            var checkOutResult = await _checkOutService.CheckOutAsync(
                objectId,
                request.Owner ?? "user@example.com",
                request.MachineName ?? Environment.MachineName,
                "Initial checkout");

            if (!checkOutResult.Success)
            {
                return BadRequest(new { message = $"Document created but checkout failed: {checkOutResult.Message}" });
            }

            return Ok(new
            {
                success = true,
                objectId = objectId,
                revision = checkOutResult.Revision,
                checkedOut = true,
                message = "Document created and checked out successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document");
            return StatusCode(500, new { message = $"Error creating document: {ex.Message}" });
        }
    }

    /// <summary>
    /// Show create document UI dialog and process the creation
    /// </summary>
    [HttpPost("create-ui")]
    public async Task<IActionResult> CreateDocumentWithUI()
    {
        _logger.LogInformation("Showing Create Document UI");

        try
        {
            // Generate ObjectId and fileName
            var objectId = $"DOC-{DateTime.Now:yyyyMMdd}-{new Random().Next(10000, 99999)}";
            var fileName = $"{objectId}.docx";

            // Show the WPF dialog
            var dialogResult = await _dialogService.ShowCreateDocumentDialogAsync(objectId, fileName);

            if (dialogResult == null || !dialogResult.Success)
            {
                _logger.LogInformation("Document creation cancelled by user");
                return Ok(new { success = false, message = "Document creation cancelled by user" });
            }

            // Create empty document content initially
            using var emptyStream = new MemoryStream();

            // Create the document in the database
            var createdObjectId = await _documentService.CreateDocumentAsync(
                emptyStream,
                dialogResult.FileName,
                dialogResult.Owner,
                dialogResult.Group,
                dialogResult.Role,
                dialogResult.Project,
                dialogResult.Comment);

            _logger.LogInformation("Document created: {ObjectId}", createdObjectId);

            // Check out the document immediately so user can edit
            var checkOutResult = await _checkOutService.CheckOutAsync(
                createdObjectId,
                dialogResult.Owner,
                Environment.MachineName,
                "Initial checkout");

            if (!checkOutResult.Success)
            {
                return BadRequest(new { message = $"Document created but checkout failed: {checkOutResult.Message}" });
            }

            return Ok(new
            {
                success = true,
                objectId = createdObjectId,
                fileName = dialogResult.FileName,
                revision = checkOutResult.Revision,
                checkedOut = true,
                message = "Document created and checked out successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during document creation with UI");
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Show save as UI dialog to bring existing document into PLM
    /// </summary>
    [HttpPost("saveas-ui")]
    public async Task<IActionResult> SaveAsWithUI([FromForm] SaveAsUIRequest request)
    {
        _logger.LogInformation("Showing Save As UI for {FileName}", request.CurrentFileName);

        try
        {
            if (request.File == null)
            {
                return BadRequest(new { message = "File is required" });
            }

            // Generate ObjectId and suggest filename
            var objectId = $"DOC-{DateTime.Now:yyyyMMdd}-{new Random().Next(10000, 99999)}";
            var suggestedFileName = string.IsNullOrEmpty(request.CurrentFileName)
                ? $"{objectId}.docx"
                : request.CurrentFileName;

            // Show the WPF dialog
            var dialogResult = await _dialogService.ShowCreateDocumentDialogAsync(objectId, suggestedFileName);

            if (dialogResult == null || !dialogResult.Success)
            {
                _logger.LogInformation("Save As cancelled by user");
                return Ok(new { success = false, message = "Save As cancelled by user" });
            }

            // Create the document in the database with the file content
            using var fileStream = request.File.OpenReadStream();

            var createdObjectId = await _documentService.CreateDocumentAsync(
                fileStream,
                dialogResult.FileName,
                dialogResult.Owner,
                dialogResult.Group,
                dialogResult.Role,
                dialogResult.Project,
                dialogResult.Comment);

            _logger.LogInformation("Document saved to PLM: {ObjectId}", createdObjectId);

            // Check out the document immediately so user can continue editing
            var checkOutResult = await _checkOutService.CheckOutAsync(
                createdObjectId,
                dialogResult.Owner,
                Environment.MachineName,
                "Initial checkout after Save As");

            if (!checkOutResult.Success)
            {
                return BadRequest(new { message = $"Document saved but checkout failed: {checkOutResult.Message}" });
            }

            return Ok(new
            {
                success = true,
                objectId = createdObjectId,
                fileName = dialogResult.FileName,
                revision = checkOutResult.Revision,
                checkedOut = true,
                message = "Document saved to PLM successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Save As with UI");
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Get document information
    /// </summary>
    [HttpGet("{objectId}")]
    public async Task<IActionResult> GetDocument(string objectId)
    {
        _logger.LogInformation("Get document: {ObjectId}", objectId);

        var document = await _documentService.GetDocumentAsync(objectId);

        if (document == null)
        {
            return NotFound(new { message = "Document not found" });
        }

        // Check if document is checked out
        var lockStatus = await _checkOutService.GetCheckOutStatusAsync(objectId);

        return Ok(new
        {
            objectId = document.ObjectId,
            fileName = document.FileName,
            revision = document.CurrentRevision,
            status = document.Status.ToString(),
            owner = document.Owner,
            project = document.Project,
            createdDate = document.CreatedDate,
            modifiedDate = document.ModifiedDate,
            isCheckedOut = lockStatus != null,
            checkedOutBy = lockStatus?.LockedBy,
            checkedOutDate = lockStatus?.LockedDate
        });
    }

    /// <summary>
    /// Download document content
    /// </summary>
    [HttpGet("{objectId}/content")]
    public async Task<IActionResult> DownloadDocument(string objectId, [FromQuery] string? revision = null)
    {
        _logger.LogInformation("Download document: {ObjectId}, revision: {Revision}", objectId, revision);

        var document = await _documentService.GetDocumentAsync(objectId);
        if (document == null)
        {
            return NotFound(new { message = "Document not found" });
        }

        var stream = await _documentService.DownloadDocumentAsync(objectId, revision);
        if (stream == null)
        {
            return NotFound(new { message = "Document content not found" });
        }

        return File(stream, "application/octet-stream", document.FileName);
    }

    /// <summary>
    /// Show open document UI dialog and return selected ObjectId
    /// </summary>
    [HttpPost("open-ui")]
    public async Task<IActionResult> OpenDocumentWithUI()
    {
        _logger.LogInformation("Showing Open Document UI");

        try
        {
            var dialogResult = await _dialogService.ShowOpenDocumentDialogAsync();

            if (dialogResult == null || !dialogResult.Success)
            {
                _logger.LogInformation("Open document cancelled by user");
                return Ok(new { success = false, message = "Open document cancelled by user" });
            }

            return Ok(new
            {
                success = true,
                objectId = dialogResult.ObjectId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during open document with UI");
            return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
        }
    }
}

public record CreateDocumentJsonRequest(
    string FileName,
    string? Owner = null,
    string? Group = null,
    string? Role = null,
    string? Project = null,
    string? MachineName = null,
    string? Comment = null);

public class SaveAsUIRequest
{
    public IFormFile? File { get; set; }
    public string? CurrentFileName { get; set; }
}

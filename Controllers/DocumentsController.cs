using Microsoft.AspNetCore.Mvc;
using FreePLM.Office.Integration.Models;
using FreePLM.Database.Services;

namespace FreePLM.Office.Integration.Controllers;

/// <summary>
/// Controller for document management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly ILogger<DocumentsController> _logger;
    private readonly IDocumentService _documentService;

    public DocumentsController(ILogger<DocumentsController> logger, IDocumentService documentService)
    {
        _logger = logger;
        _documentService = documentService;
    }

    /// <summary>
    /// Get document metadata by ObjectId
    /// </summary>
    [HttpGet("{objectId}")]
    public async Task<IActionResult> GetDocument(string objectId)
    {
        _logger.LogInformation("Getting document {ObjectId}", objectId);

        var document = await _documentService.GetDocumentAsync(objectId);

        if (document == null)
        {
            return NotFound(new { message = "Document not found" });
        }

        return Ok(document);
    }

    /// <summary>
    /// Download document file content
    /// </summary>
    [HttpGet("{objectId}/content")]
    public async Task<IActionResult> GetDocumentContent(string objectId, [FromQuery] string? revision = null)
    {
        _logger.LogInformation("Downloading document {ObjectId}, revision {Revision}", objectId, revision ?? "current");

        var fileStream = await _documentService.DownloadDocumentAsync(objectId, revision);

        if (fileStream == null)
        {
            return NotFound(new { message = "Document not found" });
        }

        var document = await _documentService.GetDocumentAsync(objectId);
        var fileName = document?.FileName ?? "document";

        return File(fileStream, "application/octet-stream", fileName);
    }

    /// <summary>
    /// Create a new document
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateDocument([FromForm] CreateDocumentRequest request)
    {
        _logger.LogInformation("Creating new document {FileName}", request.FileName);

        if (request.File == null)
        {
            return BadRequest(new { message = "File is required" });
        }

        using var fileStream = request.File.OpenReadStream();

        var objectId = await _documentService.CreateDocumentAsync(
            fileStream,
            request.FileName,
            request.Owner,
            request.Group,
            request.Role,
            request.Project,
            request.Comment);

        return Ok(new
        {
            success = true,
            objectId = objectId,
            revision = "A.01",
            message = "Document created successfully"
        });
    }

    /// <summary>
    /// Search documents
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchDocuments([FromQuery] string? objectId = null,
        [FromQuery] string? fileName = null, [FromQuery] string? project = null,
        [FromQuery] string? owner = null, [FromQuery] string? status = null)
    {
        _logger.LogInformation("Searching documents");

        var documents = await _documentService.SearchDocumentsAsync(objectId, fileName, project, owner, status);
        var documentList = documents.ToList();

        return Ok(new
        {
            documents = documentList,
            totalCount = documentList.Count,
            pageNumber = 1,
            pageSize = 50,
            totalPages = (documentList.Count + 49) / 50
        });
    }
}

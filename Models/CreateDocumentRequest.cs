namespace FreePLM.Office.Integration.Models;

/// <summary>
/// Request model for creating a new document with file upload
/// </summary>
public class CreateDocumentRequest
{
    /// <summary>
    /// File to upload
    /// </summary>
    public IFormFile? File { get; set; }

    /// <summary>
    /// Document file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Document owner
    /// </summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Document group
    /// </summary>
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// Document role
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Project name
    /// </summary>
    public string Project { get; set; } = string.Empty;

    /// <summary>
    /// Optional creation comment
    /// </summary>
    public string? Comment { get; set; }
}

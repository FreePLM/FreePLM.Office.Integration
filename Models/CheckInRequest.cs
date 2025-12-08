namespace FreePLM.Office.Integration.Models;

/// <summary>
/// Request model for check-in operation with file upload
/// </summary>
public class CheckInRequest
{
    /// <summary>
    /// Document object ID
    /// </summary>
    public string ObjectId { get; set; } = string.Empty;

    /// <summary>
    /// File to upload
    /// </summary>
    public IFormFile? File { get; set; }

    /// <summary>
    /// Check-in comment
    /// </summary>
    public string Comment { get; set; } = string.Empty;

    /// <summary>
    /// Create major revision (A→B) instead of minor revision (A.01→A.02)
    /// </summary>
    public bool CreateMajorRevision { get; set; } = false;

    /// <summary>
    /// Optional new status to set after check-in
    /// </summary>
    public string? NewStatus { get; set; }
}

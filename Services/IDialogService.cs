using System.Threading.Tasks;
using FreePLM.Office.Integration.Windows;

namespace FreePLM.Office.Integration.Services
{
    public interface IDialogService
    {
        Task<CheckInDialogResult?> ShowCheckInDialogAsync(string objectId, string fileName, string currentRevision);
        Task<CreateDocumentDialogResult?> ShowCreateDocumentDialogAsync(string objectId, string fileName);
        Task<SearchDialogResult?> ShowSearchDialogAsync();
        Task<OpenDocumentDialogResult?> ShowOpenDocumentDialogAsync();
    }

    public class CheckInDialogResult
    {
        public string ObjectId { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public bool CreateMajorRevision { get; set; }
        public byte[]? FileContent { get; set; }
        public bool Success { get; set; }
    }
}

using System;
using System.IO;
using System.Windows;
using FreePLM.Office.Integration.Services;
using FreePLM.WPF.UserControls.ViewModels;
using Serilog;

namespace FreePLM.Office.Integration.Windows
{
    public partial class CheckInDialog : Window
    {
        private readonly string _objectId;
        private readonly string _fileName;
        private readonly string _currentRevision;

        public CheckInDialogResult? Result { get; private set; }

        public CheckInDialog(string objectId, string fileName, string currentRevision)
        {
            InitializeComponent();

            _objectId = objectId;
            _fileName = fileName;
            _currentRevision = currentRevision;

            // Initialize the CheckIn control
            if (CheckInControl.ViewModel != null)
            {
                CheckInControl.ViewModel.Initialize(objectId, fileName, currentRevision);
                CheckInControl.ViewModel.CheckInRequested += OnCheckInRequested;
                CheckInControl.ViewModel.CancelRequested += OnCancelRequested;
            }
        }

        private void OnCheckInRequested(object? sender, CheckInEventArgs e)
        {
            try
            {
                // Get the file content from Word's temp location
                var tempPath = Path.Combine(Path.GetTempPath(), "FreePLM", e.ObjectId);
                var filePath = Path.Combine(tempPath, _fileName);

                if (!File.Exists(filePath))
                {
                    // Try alternative locations
                    filePath = Path.Combine(Path.GetTempPath(), _fileName);

                    if (!File.Exists(filePath))
                    {
                        MessageBox.Show(
                            $"Could not find the document file. Please ensure the document is saved.\n\nExpected location: {filePath}",
                            "File Not Found",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
                }

                // Read file with retry logic and FileShare.ReadWrite to handle lingering Word locks
                byte[] fileContent = null;
                int maxRetries = 5;
                int retryDelayMs = 500;

                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            fileContent = new byte[fileStream.Length];
                            fileStream.Read(fileContent, 0, (int)fileStream.Length);
                        }
                        break; // Success - exit retry loop
                    }
                    catch (IOException) when (i < maxRetries - 1)
                    {
                        // File still locked, wait and retry
                        System.Threading.Thread.Sleep(retryDelayMs);
                    }
                }

                if (fileContent == null)
                {
                    throw new IOException($"Unable to read file after {maxRetries} attempts: {filePath}");
                }

                Result = new CheckInDialogResult
                {
                    ObjectId = e.ObjectId,
                    Comment = e.Comment,
                    CreateMajorRevision = e.CreateMajorRevision,
                    FileContent = fileContent,
                    Success = true
                };

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to prepare check-in");
                MessageBox.Show(
                    $"Failed to prepare check-in: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnCancelRequested(object? sender, EventArgs e)
        {
            Result = null;
            DialogResult = false;
            Close();
        }
    }
}

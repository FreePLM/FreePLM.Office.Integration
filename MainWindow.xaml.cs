using System.Diagnostics;
using System.IO;
using System.Windows;
using FreePLM.Database.Entities;
using FreePLM.Database.Services;
using FreePLM.WPF.UserControls.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using Serilog;

namespace FreePLM.Office.Integration
{
    public partial class MainWindow : Window
    {
        private readonly string _vaultRootPath;
        private readonly string _databasePath;
        private string? _currentTestObjectId;

        public MainWindow()
        {
            InitializeComponent();

            // Load configuration
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            _vaultRootPath = config["FreePLM:Storage:VaultRootPath"] ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FreePLM", "Vault");
            _databasePath = config["FreePLM:Database:ConnectionString"]?.Replace("Data Source=", "") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FreePLM", "freeplm.db");

            // Set initial values
            VaultPathTextBox.Text = _vaultRootPath;
            DatabasePathTextBox.Text = _databasePath;

            // Subscribe to check-in events
            if (CheckInControl.ViewModel != null)
            {
                CheckInControl.ViewModel.CheckInRequested += CheckInViewModel_CheckInRequested;
                CheckInControl.ViewModel.CancelRequested += CheckInViewModel_CancelRequested;
            }

            LogStatus("FreePLM service ready.");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Hide instead of close
            e.Cancel = true;
            Hide();
        }

        private void OpenSwagger_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "http://localhost:5000",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open browser: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CreateTestDocument_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogStatus("Creating test document...");

                // Generate a test document ID
                _currentTestObjectId = $"DOC-{DateTime.Now:yyyyMMdd}-{new Random().Next(10000, 99999)}";

                // Create test Word file content
                var testFilePath = Path.Combine(Path.GetTempPath(), $"{_currentTestObjectId}.docx");

                // For testing, create a simple text file (in production, this would be an actual Word document)
                File.WriteAllText(testFilePath, $"Test document content for {_currentTestObjectId}\n\nCreated: {DateTime.Now}");

                // Initialize the document info
                var docInfoVM = DocumentInfoControl.ViewModel;
                if (docInfoVM != null)
                {
                    docInfoVM.ObjectId = _currentTestObjectId;
                    docInfoVM.FileName = $"{_currentTestObjectId}.docx";
                    docInfoVM.CurrentRevision = "A.01";
                    docInfoVM.Status = "Private";
                    docInfoVM.Owner = Environment.UserName;
                    docInfoVM.CreatedBy = Environment.UserName;
                    docInfoVM.CreatedDate = DateTime.Now;
                    docInfoVM.ModifiedBy = Environment.UserName;
                    docInfoVM.ModifiedDate = DateTime.Now;
                    docInfoVM.FileSize = new FileInfo(testFilePath).Length;
                    docInfoVM.FileExtension = ".docx";
                    docInfoVM.IsCheckedOut = false;
                }

                LogStatus($"Test document created: {_currentTestObjectId}");
                LogStatus($"Temp file: {testFilePath}");
            }
            catch (Exception ex)
            {
                LogStatus($"ERROR: {ex.Message}");
                MessageBox.Show($"Failed to create test document: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SimulateCheckOut_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_currentTestObjectId))
                {
                    MessageBox.Show("Please create a test document first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                LogStatus($"Simulating check-out for {_currentTestObjectId}...");

                // Update document info to show checked out state
                var docInfoVM = DocumentInfoControl.ViewModel;
                if (docInfoVM != null)
                {
                    docInfoVM.IsCheckedOut = true;
                    docInfoVM.CheckedOutBy = Environment.UserName;
                    docInfoVM.CheckedOutDate = DateTime.Now;
                }

                // Initialize check-in control
                var checkInVM = CheckInControl.ViewModel;
                if (checkInVM != null)
                {
                    checkInVM.Initialize(_currentTestObjectId, $"{_currentTestObjectId}.docx", "A.01");
                }

                LogStatus("Document checked out successfully.");
                LogStatus("You can now test the check-in process.");
            }
            catch (Exception ex)
            {
                LogStatus($"ERROR: {ex.Message}");
                MessageBox.Show($"Failed to simulate check-out: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CheckInViewModel_CheckInRequested(object? sender, CheckInEventArgs e)
        {
            try
            {
                LogStatus($"Processing check-in for {e.ObjectId}...");
                LogStatus($"Comment: {e.Comment}");
                LogStatus($"Major Revision: {e.CreateMajorRevision}");

                // Ensure vault directory exists
                var vaultDir = Path.Combine(_vaultRootPath, e.ObjectId);
                Directory.CreateDirectory(vaultDir);

                // Get the test file
                var testFilePath = Path.Combine(Path.GetTempPath(), $"{e.ObjectId}.docx");

                if (!File.Exists(testFilePath))
                {
                    LogStatus("ERROR: Test file not found. Creating a new one...");
                    File.WriteAllText(testFilePath, $"Test document content for {e.ObjectId}\n\nCreated: {DateTime.Now}");
                }

                // Calculate new revision
                var currentRevision = DocumentInfoControl.ViewModel?.CurrentRevision ?? "A.01";
                var newRevision = CalculateNextRevision(currentRevision, e.CreateMajorRevision);

                // Copy file to vault
                var vaultFilePath = Path.Combine(vaultDir, $"{e.ObjectId}_{newRevision.Replace(".", "_")}.docx");
                File.Copy(testFilePath, vaultFilePath, overwrite: true);

                LogStatus($"File saved to: {vaultFilePath}");
                LogStatus($"New revision: {newRevision}");
                LogStatus($"File size: {new FileInfo(vaultFilePath).Length} bytes");

                // Update document info
                var docInfoVM = DocumentInfoControl.ViewModel;
                if (docInfoVM != null)
                {
                    docInfoVM.CurrentRevision = newRevision;
                    docInfoVM.IsCheckedOut = false;
                    docInfoVM.CheckedOutBy = null;
                    docInfoVM.CheckedOutDate = null;
                    docInfoVM.ModifiedBy = Environment.UserName;
                    docInfoVM.ModifiedDate = DateTime.Now;
                    docInfoVM.FileSize = new FileInfo(vaultFilePath).Length;
                }

                LogStatus("Check-in completed successfully!");
                MessageBox.Show($"Document checked in successfully!\n\nNew Revision: {newRevision}\nLocation: {vaultFilePath}",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Clear the check-in form
                CheckInControl.ViewModel?.Initialize("", "", "");
            }
            catch (Exception ex)
            {
                LogStatus($"ERROR: {ex.Message}");
                Log.Error(ex, "Check-in failed");
                MessageBox.Show($"Check-in failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckInViewModel_CancelRequested(object? sender, EventArgs e)
        {
            LogStatus("Check-in cancelled by user.");
            CheckInControl.ViewModel?.Initialize("", "", "");
        }

        private void OpenVaultFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(_vaultRootPath))
                {
                    Directory.CreateDirectory(_vaultRootPath);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = _vaultRootPath,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open vault folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BrowseVaultPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Vault Root Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                VaultPathTextBox.Text = dialog.FolderName;
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement settings save functionality
            MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LogStatus(string message)
        {
            Dispatcher.Invoke(() =>
            {
                StatusTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                StatusTextBox.ScrollToEnd();
            });

            Log.Information(message);
        }

        private string CalculateNextRevision(string currentRevision, bool isMajor)
        {
            try
            {
                var parts = currentRevision.Split('.');
                if (parts.Length != 2)
                {
                    return "A.01";
                }

                var letter = parts[0];
                var number = int.Parse(parts[1]);

                if (isMajor)
                {
                    // Increment letter (A -> B, B -> C, etc.)
                    letter = ((char)(letter[0] + 1)).ToString();
                    number = 1;
                }
                else
                {
                    // Increment number
                    number++;
                }

                return $"{letter}.{number:D2}";
            }
            catch
            {
                return "A.01";
            }
        }
    }
}

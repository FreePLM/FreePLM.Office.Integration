using System;
using System.Windows;
using FreePLM.WPF.UserControls.ViewModels;
using Serilog;

namespace FreePLM.Office.Integration.Windows
{
    public partial class CreateDocumentDialog : Window
    {
        private readonly string _objectId;
        private readonly string _fileName;

        public CreateDocumentDialogResult? Result { get; private set; }

        public CreateDocumentDialog(string objectId, string fileName)
        {
            InitializeComponent();

            _objectId = objectId;
            _fileName = fileName;

            // Initialize the CreateDocument control
            if (CreateDocumentControl.ViewModel != null)
            {
                CreateDocumentControl.ViewModel.Initialize(objectId, fileName);
                CreateDocumentControl.ViewModel.CreateRequested += OnCreateRequested;
                CreateDocumentControl.ViewModel.CancelRequested += OnCancelRequested;
            }
        }

        private void OnCreateRequested(object? sender, CreateDocumentEventArgs e)
        {
            try
            {
                Result = new CreateDocumentDialogResult
                {
                    ObjectId = e.ObjectId,
                    FileName = e.FileName,
                    Project = e.Project,
                    Owner = e.Owner,
                    Group = e.Group,
                    Role = e.Role,
                    Comment = e.Comment,
                    Success = true
                };

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to prepare document creation");
                MessageBox.Show(
                    $"Failed to prepare document creation: {ex.Message}",
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

    public class CreateDocumentDialogResult
    {
        public string ObjectId { get; set; }
        public string FileName { get; set; }
        public string Project { get; set; }
        public string Owner { get; set; }
        public string Group { get; set; }
        public string Role { get; set; }
        public string Comment { get; set; }
        public bool Success { get; set; }
    }
}

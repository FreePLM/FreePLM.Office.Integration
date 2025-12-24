using System;
using System.Windows;
using FreePLM.WPF.UserControls.ViewModels;
using Serilog;

namespace FreePLM.Office.Integration.Windows
{
    public partial class OpenDocumentDialog : Window
    {
        public OpenDocumentDialogResult? Result { get; private set; }

        public OpenDocumentDialog()
        {
            InitializeComponent();

            // Subscribe to ViewModel events
            if (OpenDocumentControl.ViewModel != null)
            {
                OpenDocumentControl.ViewModel.OpenRequested += OnOpenRequested;
                OpenDocumentControl.ViewModel.CancelRequested += OnCancelRequested;
            }
        }

        private void OnOpenRequested(object? sender, OpenDocumentEventArgs e)
        {
            try
            {
                Result = new OpenDocumentDialogResult
                {
                    ObjectId = e.ObjectId,
                    Success = true
                };

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to prepare document open");
                MessageBox.Show(
                    $"Failed to prepare document open: {ex.Message}",
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

    public class OpenDocumentDialogResult
    {
        public string ObjectId { get; set; }
        public bool Success { get; set; }
    }
}

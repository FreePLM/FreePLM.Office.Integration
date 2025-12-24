using System;
using System.Linq;
using System.Windows;
using FreePLM.Database.Services;
using FreePLM.WPF.UserControls.ViewModels;
using Serilog;

namespace FreePLM.Office.Integration.Windows
{
    public partial class SearchDialog : Window
    {
        private readonly IDocumentService _documentService;

        public SearchDialogResult? Result { get; private set; }

        public SearchDialog(IDocumentService documentService)
        {
            InitializeComponent();

            _documentService = documentService;

            // Subscribe to ViewModel events
            if (SearchDocumentControl.ViewModel != null)
            {
                SearchDocumentControl.ViewModel.SearchRequested += OnSearchRequested;
                SearchDocumentControl.ViewModel.OpenRequested += OnOpenRequested;
                SearchDocumentControl.ViewModel.CancelRequested += OnCancelRequested;
            }
        }

        private async void OnSearchRequested(object? sender, DocumentSearchEventArgs e)
        {
            try
            {
                Log.Information("Searching documents with ObjectId={ObjectId}, FileName={FileName}, Project={Project}, Owner={Owner}, Status={Status}",
                    e.ObjectId, e.FileName, e.Project, e.Owner, e.Status);

                // Perform the search using the document service
                var documents = await _documentService.SearchDocumentsAsync(
                    e.ObjectId,
                    e.FileName,
                    e.Project,
                    e.Owner,
                    e.Status);

                Log.Information("Search returned {Count} documents", documents.Count());

                // Convert to ViewModel format
                var results = documents.Select(doc => new DocumentSearchResult
                {
                    ObjectId = doc.ObjectId,
                    FileName = doc.FileName,
                    Revision = doc.CurrentRevision,
                    Status = doc.Status.ToString(),
                    Owner = doc.Owner,
                    Project = doc.Project,
                    ModifiedDate = doc.ModifiedDate,
                    IsCheckedOut = doc.IsCheckedOut,
                    CheckedOutBy = doc.CheckedOutBy
                }).ToList();

                SearchDocumentControl.ViewModel.SetSearchResults(results);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to perform search");
                MessageBox.Show(
                    $"Failed to perform search: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                SearchDocumentControl.ViewModel.SetSearchResults(Enumerable.Empty<DocumentSearchResult>());
            }
        }

        private void OnOpenRequested(object? sender, DocumentSearchEventArgs e)
        {
            try
            {
                Result = new SearchDialogResult
                {
                    ObjectId = e.ObjectId,
                    FileName = e.FileName,
                    Success = true
                };

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to open document");
                MessageBox.Show(
                    $"Failed to open document: {ex.Message}",
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

        public void SetSearchResults(System.Collections.Generic.IEnumerable<DocumentSearchResult> results)
        {
            SearchDocumentControl.ViewModel?.SetSearchResults(results);
        }
    }

    public class SearchDialogResult
    {
        public string ObjectId { get; set; }
        public string FileName { get; set; }
        public bool Success { get; set; }
    }
}

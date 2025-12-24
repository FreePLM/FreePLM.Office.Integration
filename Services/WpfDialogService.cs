using System;
using System.Threading.Tasks;
using System.Windows;
using FreePLM.Database.Services;
using FreePLM.Office.Integration.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace FreePLM.Office.Integration.Services
{
    public class WpfDialogService : IDialogService
    {
        private readonly IServiceProvider _serviceProvider;

        public WpfDialogService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task<CheckInDialogResult?> ShowCheckInDialogAsync(string objectId, string fileName, string currentRevision)
        {
            var tcs = new TaskCompletionSource<CheckInDialogResult?>();

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var dialog = new CheckInDialog(objectId, fileName, currentRevision);
                    var result = dialog.ShowDialog();

                    if (result == true && dialog.Result != null)
                    {
                        tcs.SetResult(dialog.Result);
                    }
                    else
                    {
                        tcs.SetResult(null);
                    }
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        public Task<CreateDocumentDialogResult?> ShowCreateDocumentDialogAsync(string objectId, string fileName)
        {
            var tcs = new TaskCompletionSource<CreateDocumentDialogResult?>();

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var dialog = new CreateDocumentDialog(objectId, fileName);
                    var result = dialog.ShowDialog();

                    if (result == true && dialog.Result != null)
                    {
                        tcs.SetResult(dialog.Result);
                    }
                    else
                    {
                        tcs.SetResult(null);
                    }
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        public Task<SearchDialogResult?> ShowSearchDialogAsync()
        {
            var tcs = new TaskCompletionSource<SearchDialogResult?>();

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    // Resolve the scoped service from the service provider
                    using var scope = _serviceProvider.CreateScope();
                    var documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();

                    var dialog = new SearchDialog(documentService);
                    var result = dialog.ShowDialog();

                    if (result == true && dialog.Result != null)
                    {
                        tcs.SetResult(dialog.Result);
                    }
                    else
                    {
                        tcs.SetResult(null);
                    }
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }

        public Task<OpenDocumentDialogResult?> ShowOpenDocumentDialogAsync()
        {
            var tcs = new TaskCompletionSource<OpenDocumentDialogResult?>();

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var dialog = new OpenDocumentDialog();
                    var result = dialog.ShowDialog();

                    if (result == true && dialog.Result != null)
                    {
                        tcs.SetResult(dialog.Result);
                    }
                    else
                    {
                        tcs.SetResult(null);
                    }
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }
    }
}

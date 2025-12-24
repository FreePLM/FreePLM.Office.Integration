using System.IO;
using System.Windows;
using FreePLM.Office.Integration.Extensions;
using FreePLM.Office.Integration.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Serilog;

namespace FreePLM.Office.Integration
{
    public partial class App : Application
    {
        private IHost? _apiHost;
        private MainWindow? _mainWindow;

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                    .Build())
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/freeplm-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting FreePLM Office Integration Service");

                // Start the API host
                await StartApiHostAsync();

                // Show main window
                ShowMainWindow();

                Log.Information("FreePLM Service started successfully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to start FreePLM Service");
                MessageBox.Show(
                    $"Failed to start FreePLM Service:\n\n{ex.Message}",
                    "FreePLM Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
            }
        }

        private async Task StartApiHostAsync()
        {
            var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder();

            // Add Serilog
            builder.Services.AddSerilog();

            // Get port from configuration
            var port = builder.Configuration.GetValue<int>("FreePLM:Server:Port", 5000);

            // Configure Kestrel to listen on HTTP
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenLocalhost(port);
            });

            // Configure services
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "FreePLM Office Integration API",
                    Version = "v1",
                    Description = "REST API for FreePLM Office document management",
                    Contact = new OpenApiContact
                    {
                        Name = "FreePLM",
                        Url = new Uri("https://github.com/FreePLM")
                    }
                });

                options.OperationFilter<FileUploadOperationFilter>();
            });

            // Add FreePLM services
            builder.Services.AddFreePLMServices(builder.Configuration);
            builder.Services.AddFreePLMCors(builder.Configuration);

            // Build the web application
            var app = builder.Build();

            // Configure middleware pipeline
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseCors("FreePLMPolicy");
            app.UseRouting();
            app.MapControllers();

            // Start the web application in the background
            _ = Task.Run(async () => await app.RunAsync());

            _apiHost = app as IHost;

            // Give it a moment to start
            await Task.Delay(500);

            Log.Information("FreePLM API listening on http://localhost:{Port}", port);
        }

        private void ShowMainWindow()
        {
            if (_mainWindow == null || !_mainWindow.IsLoaded)
            {
                _mainWindow = new MainWindow();
            }

            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
        }

        private async void Application_Exit(object sender, ExitEventArgs e)
        {
            Log.Information("Shutting down FreePLM Service");

            // Stop the API host
            if (_apiHost != null)
            {
                await _apiHost.StopAsync();
                _apiHost.Dispose();
            }

            Log.CloseAndFlush();
        }
    }
}

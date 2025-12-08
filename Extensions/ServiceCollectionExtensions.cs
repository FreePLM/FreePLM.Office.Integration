using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using FreePLM.Database;
using FreePLM.Database.Repositories;
using FreePLM.Database.Repositories.Sqlite;
using FreePLM.Database.Services;
using FreePLM.Database.Storage;

namespace FreePLM.Office.Integration.Extensions;

/// <summary>
/// Extension methods for configuring FreePLM services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add FreePLM services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddFreePLMServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration
        services.AddSingleton(configuration);

        // Get configuration values
        var connectionString = configuration.GetValue<string>("FreePLM:Database:SQLite:ConnectionString")
            ?? throw new InvalidOperationException("Database connection string not configured");
        var storagePath = configuration.GetValue<string>("FreePLM:Storage:LocalDirectory:RootPath")
            ?? throw new InvalidOperationException("Storage path not configured");

        // Initialize database
        var dbInitializer = new DatabaseInitializer(connectionString);
        dbInitializer.InitializeAsync().Wait();

        // Register repositories
        services.AddScoped<IDocumentRepository>(sp => new SqliteDocumentRepository(connectionString));
        services.AddScoped<IRevisionRepository>(sp => new SqliteRevisionRepository(connectionString));
        services.AddScoped<ICheckOutRepository>(sp => new SqliteCheckOutRepository(connectionString));
        services.AddScoped<IWorkflowRepository>(sp => new SqliteWorkflowRepository(connectionString));

        // Register storage service
        services.AddScoped<IFileStorageService>(sp => new LocalDirectoryStorageService(storagePath));

        // Register PLM services
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<ICheckOutService, CheckOutService>();
        services.AddScoped<IWorkflowService, WorkflowService>();

        return services;
    }

    /// <summary>
    /// Add CORS policy for Office add-ins
    /// </summary>
    public static IServiceCollection AddFreePLMCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("FreePLM:Server:AllowedOrigins")
            .Get<string[]>() ?? new[] { "http://localhost" };

        services.AddCors(options =>
        {
            options.AddPolicy("FreePLMPolicy", builder =>
            {
                builder
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }
}

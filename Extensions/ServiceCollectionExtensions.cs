using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

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

        // TODO: Register FreePLM.Common interfaces and implementations
        // services.AddScoped<IDocumentService, DocumentService>();
        // services.AddScoped<ICheckOutService, CheckOutService>();
        // services.AddScoped<IWorkflowService, WorkflowService>();
        // services.AddScoped<IVersionService, VersionService>();
        // services.AddScoped<IOwnershipService, OwnershipService>();

        // TODO: Register repositories
        // services.AddScoped<IPLMRepository, SQLiteRepository>();

        // TODO: Register storage providers
        // services.AddScoped<IStorageProvider, LocalDirectoryStorageProvider>();
        // services.AddScoped<IMetadataStorage, JsonMetadataStorage>();

        // TODO: Register transaction coordinator
        // services.AddScoped<ITransactionCoordinator, TransactionCoordinator>();

        // TODO: Register validation service
        // services.AddScoped<IValidationService, ValidationService>();

        // TODO: Register utility services
        // services.AddSingleton<IObjectIdGenerator, ObjectIdGenerator>();
        // services.AddSingleton<IHashService, SHA256HashService>();

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

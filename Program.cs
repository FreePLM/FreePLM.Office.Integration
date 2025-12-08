using FreePLM.Office.Integration.Extensions;
using Microsoft.OpenApi.Models;
using Serilog;

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

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Get port from configuration
    var port = builder.Configuration.GetValue<int>("FreePLM:Server:Port", 5000);
    builder.WebHost.UseUrls($"http://localhost:{port}");

    // Add services to the container
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

        // Support for file uploads in Swagger
        options.OperationFilter<FileUploadOperationFilter>();
    });

    // Add FreePLM services
    builder.Services.AddFreePLMServices(builder.Configuration);
    builder.Services.AddFreePLMCors(builder.Configuration);

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "FreePLM API v1");
            options.RoutePrefix = string.Empty; // Serve Swagger at root
        });
    }

    app.UseSerilogRequestLogging();
    app.UseCors("FreePLMPolicy");
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("FreePLM Service listening on http://localhost:{Port}", port);
    Log.Information("Swagger UI available at http://localhost:{Port}", port);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

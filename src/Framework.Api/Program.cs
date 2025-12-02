using System.Text.Json.Serialization;
using Framework.Api.Extensions;
using Framework.Api.Middleware;
using Framework.Application;
using Framework.Infrastructure;
using Framework.Infrastructure.BackgroundJobs;
using Framework.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilogConfiguration();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Add API versioning
builder.Services.AddApiVersioningConfiguration();

// Add Swagger
builder.Services.AddSwaggerConfiguration();

// Add Application services (MediatR, FluentValidation)
builder.Services.AddApplicationServices();

// Add Infrastructure services (DbContext, Repositories)
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add Background Jobs (Hangfire)
builder.Services.AddBackgroundJobs(builder.Configuration);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerConfiguration();
}

// CORS must be before other middleware to ensure headers are added even on errors
app.UseCors("AllowAll");

// Global exception handling
app.UseGlobalExceptionHandler();

// Serilog request logging
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
   .WithName("Health")
   .WithTags("Health");

// Run database migrations and seed data in development
if (app.Environment.IsDevelopment())
{
    await app.Services.MigrateAndSeedDatabaseAsync();
}

Log.Information("Application starting up...");

try
{
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

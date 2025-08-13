using HubRocksApi.Services;
using HubRocksApi.Configuration;
using HubRocksApi.Middleware;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Override BaseUrl from environment if provided BEFORE binding options
var baseUrlFromEnv = Environment.GetEnvironmentVariable("BASE_URL");
if (!string.IsNullOrEmpty(baseUrlFromEnv))
{
    builder.Configuration["AppConfig:Api:BaseUrl"] = baseUrlFromEnv;
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure HTTP client
builder.Services.AddHttpClient();

// Configure app settings
builder.Services.Configure<AppConfig>(builder.Configuration.GetSection("AppConfig"));

// Register services
builder.Services.AddScoped<ICourseService, CourseService>();

// Add CORS with origin checking
var appConfig = builder.Configuration.GetSection("AppConfig").Get<AppConfig>();

// Get allowed origins from configuration or environment variable
var allowedOrigins = new List<string>();

// Check environment variable first (comma-separated list)
var envOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS");
if (!string.IsNullOrEmpty(envOrigins))
{
    allowedOrigins.AddRange(envOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(origin => origin.Trim()));
}
// Fallback to configuration
else if (appConfig?.AllowedOrigins?.Any() == true)
{
    allowedOrigins.AddRange(appConfig.AllowedOrigins);
}

// BaseUrl override is handled above via builder.Configuration

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        if (allowedOrigins.Any())
        {
            policy.WithOrigins(allowedOrigins.ToArray())
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
        else
        {
            // Fallback to allow any origin if no specific origins are configured
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// Log allowed origins for debugging
var logger = app.Services.GetRequiredService<ILogger<Program>>();
if (allowedOrigins.Any())
{
    logger.LogInformation("CORS configured with allowed origins: {Origins}", string.Join(", ", allowedOrigins));
}
else
{
    logger.LogWarning("CORS configured to allow any origin (not recommended for production)");
}

// Log final API BaseUrl being used (after DI options binding)
var resolvedOptions = app.Services.GetRequiredService<IOptions<AppConfig>>().Value;
logger.LogInformation("API BaseUrl in use: {BaseUrl}", resolvedOptions.Api.BaseUrl);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<OriginValidationMiddleware>();
app.UseCors("AllowedOrigins");
app.UseAuthorization();
app.MapControllers();

app.Run();
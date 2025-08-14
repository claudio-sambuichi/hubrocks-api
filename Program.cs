using HubRocksApi.Services;
using HubRocksApi.Configuration;
using HubRocksApi.Middleware;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Override BaseUrl from environment if provided BEFORE binding options
var baseUrlFromEnv = Environment.GetEnvironmentVariable("BASE_URL");
if (!string.IsNullOrEmpty(baseUrlFromEnv))
{
    builder.Configuration["AppConfig:Api:BaseUrl"] = baseUrlFromEnv;
}

// Override CacheTtlSeconds from environment if provided BEFORE binding options
var cacheTtlSecondsFromEnv = Environment.GetEnvironmentVariable("CACHE_TTL_SECONDS");
if (!string.IsNullOrEmpty(cacheTtlSecondsFromEnv))
{
    builder.Configuration["AppConfig:CacheTtlSeconds"] = cacheTtlSecondsFromEnv;
}

// Override CachingEnabled from environment if provided BEFORE binding options
var cachingEnabledFromEnv = Environment.GetEnvironmentVariable("CACHING_ENABLED");
if (!string.IsNullOrEmpty(cachingEnabledFromEnv))
{
    if (bool.TryParse(cachingEnabledFromEnv, out bool cachingEnabled))
    {
        builder.Configuration["AppConfig:CachingEnabled"] = cachingEnabled.ToString();
    }
}

// Override ApiKey from environment if provided BEFORE binding options
var apiKeyFromEnv = Environment.GetEnvironmentVariable("API_KEY");
if (!string.IsNullOrEmpty(apiKeyFromEnv))
{
    builder.Configuration["AppConfig:Api:ApiKey"] = apiKeyFromEnv;
}

// Override page size header value from environment if provided BEFORE binding options
var limitFromEnv = Environment.GetEnvironmentVariable("LIMIT");
if (!string.IsNullOrEmpty(limitFromEnv))
{
    builder.Configuration["AppConfig:Api:PageSize"] = limitFromEnv;
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure HTTP client
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

// Configure app settings
builder.Services.Configure<AppConfig>(builder.Configuration.GetSection("AppConfig"));

// Get app config for conditional caching setup
var appConfigForCaching = builder.Configuration.GetSection("AppConfig").Get<AppConfig>();

// Output caching - only add if enabled
if (appConfigForCaching?.CachingEnabled == true)
{
    builder.Services.AddOutputCache(options =>
    {
        options.AddPolicy("CoursesPolicy", policy =>
            policy.Expire(TimeSpan.FromSeconds(60))
                  .SetVaryByHeader("ie_id")
                  .SetVaryByHeader("couponId"));
    });
}

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
logger.LogInformation("Caching enabled: {CachingEnabled}", resolvedOptions.CachingEnabled);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<OriginValidationMiddleware>();
app.UseCors("AllowedOrigins");

// Use output cache middleware only if caching is enabled
if (resolvedOptions.CachingEnabled)
{
    app.UseOutputCache();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
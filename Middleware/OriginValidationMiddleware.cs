using HubRocksApi.Configuration;
using Microsoft.Extensions.Options;
using System.Net;

namespace HubRocksApi.Middleware
{
    public class OriginValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<OriginValidationMiddleware> _logger;
        private readonly List<string> _allowedOrigins;

        public OriginValidationMiddleware(
            RequestDelegate next, 
            ILogger<OriginValidationMiddleware> logger,
            IOptions<AppConfig> appConfig)
        {
            _next = next;
            _logger = logger;
            
            // Get allowed origins from configuration or environment variable
            _allowedOrigins = new List<string>();
            
            // Check environment variable first (comma-separated list)
            var envOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS");
            if (!string.IsNullOrEmpty(envOrigins))
            {
                _allowedOrigins.AddRange(envOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(origin => origin.Trim()));
            }
            // Fallback to configuration
            else if (appConfig.Value?.AllowedOrigins?.Any() == true)
            {
                _allowedOrigins.AddRange(appConfig.Value.AllowedOrigins);
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip validation if no specific origins are configured (fallback to allow all)
            if (!_allowedOrigins.Any())
            {
                await _next(context);
                return;
            }

            // Get the Origin header
            var origin = context.Request.Headers["Origin"].FirstOrDefault();

            // Validate origin against allowed list
            if (!string.IsNullOrEmpty(origin) && !_allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Blocked request from unauthorized origin: {Origin}. Allowed origins: {AllowedOrigins}", 
                    origin, string.Join(", ", _allowedOrigins));

                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                context.Response.ContentType = "application/json";
                
                var errorResponse = new
                {
                    error = "Forbidden",
                    message = "Not allowed",
                    statusCode = 403,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                };

                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResponse));
                return;
            }

            // Origin is valid, continue with the request
            await _next(context);
        }
    }
}
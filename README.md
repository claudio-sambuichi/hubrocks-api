# HubRocks API

A .NET 8 Web API that aggregates courses from multiple educational institutions via external API.

## Features

- **Multi-Institution Support**: Fetches courses from multiple educational institutions
- **External API Integration**: Connects to external course providers
- **Configurable**: Easy configuration through appsettings.json
- **RESTful API**: Clean REST endpoints for courses and institutions
- **Swagger Documentation**: Built-in API documentation
- **Logging**: Comprehensive logging throughout the application
- **Caching**: Output caching (per-request) and in-memory response caching with configurable TTL

## API Endpoints

### Courses
- `GET /api/courses` - Get all courses from external API

**Headers:**
- `ie_id`: Institution ID (required, integer) - must be provided
- `couponId`: Coupon ID (optional, string) - for coupon-specific queries

**Response Format:**
```json
[
  {
    "id": "course-id",
    "title": "Course Title",
    "ie": "1",
    "category": "Category",
    "type": "MBA",
    "thumb": "image-url",
    "link": "course-url",
    "price": "100.00",
    "old_price": "150.00"
  }
]
```

## Configuration

Update `appsettings.json` to configure the API (prefer environment variables for secrets like API keys):

```json
{
  "AppConfig": {
    "Api": {
      "BaseUrl": "https://your-api-base-url.com",
      "ApiKey": "" // Set via environment variable `API_KEY`
    },
    "AllowedOrigins": [
      "https://yourdomain.com",
      "https://localhost:3000"
    ]
  }
}
```

### Configuration Options

- `Api.BaseUrl`: Base URL for the external API
- `Api.ApiKey`: API key for authentication (recommended: set via environment variable)
- `Api.PageSize`: Page size sent via `limit` header to the external API (optional; default 20)
- `AllowedOrigins`: Array of allowed origin domains for CORS (optional)
- `CachingEnabled`: Whether to enable output caching (optional; default true)
- `CacheTtlSeconds`: In-memory cache TTL in seconds (optional; default 300)

### Environment Variables

For deployment flexibility, you can override configuration using environment variables:

- `API_KEY`: API key used for external API calls (overrides `AppConfig:Api:ApiKey`)
- `LIMIT`: Overrides the `limit` header value sent to the external API (overrides `AppConfig:Api:PageSize`)
- `BASE_URL`: External API base URL (overrides `AppConfig:Api:BaseUrl`)
- `CACHING_ENABLED`: Whether to enable output caching (overrides `AppConfig:CachingEnabled`; accepts "true"/"false"; default true)
- `CACHE_TTL_SECONDS`: TTL (in seconds) for in-memory caching of course lists (overrides `AppConfig:CacheTtlSeconds`; default 300)
- `ALLOWED_ORIGINS`: Comma-separated list of allowed origins (e.g., "https://domain1.com,https://domain2.com")

**Example:**
```bash
# Windows PowerShell
$env:API_KEY = "<your-key>"
$env:BASE_URL = "https://api.example.com"
$env:CACHING_ENABLED = "true"
$env:CACHE_TTL_SECONDS = "300"
$env:ALLOWED_ORIGINS = "https://prod.example.com,https://staging.example.com"

# Linux/macOS/WSL
export API_KEY="<your-key>"
export BASE_URL="https://api.example.com"
export CACHING_ENABLED="true"
export CACHE_TTL_SECONDS="300"
export ALLOWED_ORIGINS="https://prod.example.com,https://staging.example.com"
```

## Caching

The API uses two complementary caching layers to improve performance and reduce load on the upstream API:

### 1) Output Caching (per-request response cache)
- Enabled on `GET /api/courses` via a policy named `CoursesPolicy` (configurable via `CACHING_ENABLED`)
- **Expiration**: 60 seconds
- **Vary-By Headers**: `ie_id`, `couponId`
- Effect: identical requests (same headers) within 60 seconds are served from an output cache without re-executing the action
- **Configuration**: Can be disabled by setting `CACHING_ENABLED=false` or `AppConfig:CachingEnabled=false`

### 2) In-Memory Caching (upstream data cache)
- The service caches the fetched course list in memory (configurable via `CACHING_ENABLED`)
- **Cache Key**: `courses:{ie_id}:{couponId or _}`
- **TTL**: Configurable via `CacheTtlSeconds` (default 300s); can be overridden by `CACHE_TTL_SECONDS`
- Effect: reduces repeated calls to the external API across requests
- **Configuration**: Can be disabled by setting `CACHING_ENABLED=false` or `AppConfig:CachingEnabled=false`

Notes:
- Output caching is per HTTP response, while in-memory caching de-duplicates upstream fetches across requests
- Both caching layers are controlled by the same `CACHING_ENABLED` setting
- To adjust only the in-memory cache duration, set `CACHE_TTL_SECONDS`
- When caching is disabled, all requests will fetch fresh data from the external API

## Running the Application

1. **Prerequisites**
   - .NET 8 SDK
   - Visual Studio 2022 or VS Code

2. **Run the application**
   ```bash
   dotnet run
   ```

3. **Access Swagger UI**
   - Navigate to `https://localhost:7xxx/swagger` (port may vary)

## Project Structure

```
HubRocksApi/
├── Controllers/           # API Controllers
├── Services/             # Business logic services
├── Models/               # Data models (Course, ApiCourse, ApiResponse)
├── Configuration/        # Configuration classes
├── Middleware/           # Custom middleware (Origin validation)
├── Program.cs           # Application entry point
└── appsettings.json     # Configuration file
```

## Data Models

### Course
- Complete course information including pricing, institution, category
- Supports best seller flags and discount pricing

### Institution
- Institution details with coupon support
- Active/inactive status management

### API Integration
- Transforms external API responses to internal course format
- Handles pagination and institution-specific queries

## Security

### Origin Validation

The API implements strict origin checking with two layers of security:

#### 1. Server-Side Origin Validation (Middleware)
- **Validates Origin header** on every request
- **Returns 403 Forbidden** for unauthorized origins
- **Logs blocked attempts** for security monitoring
- **JSON error response** with detailed information

#### 2. CORS (Cross-Origin Resource Sharing)
- **Browser-level protection** for web applications
- **Preflight request handling** for complex requests
- **Credentials support** for authenticated requests

**Configuration Priority:**
1. `ALLOWED_ORIGINS` environment variable
2. `AllowedOrigins` in appsettings.json
3. Fallback to allow any origin (with warnings)

**Error Response Format:**
```json
{
  "error": "Forbidden",
  "message": "Origin not allowed",
  "statusCode": 403,
  "timestamp": "2024-01-01T12:00:00.000Z"
}
```

### Best Practices

- Always configure specific allowed origins in production
- Use HTTPS origins in production environments
- Monitor origin validation logs for unauthorized access attempts
- Requests without Origin header (same-origin/direct API calls) are allowed

## Error Handling

The API includes comprehensive error handling:
- HTTP status codes for different error types
- Detailed logging for debugging
- Returns empty results when external APIs fail
- CORS violations are logged and blocked

## Development

### API Usage Examples

**Get all courses (institution required):**
```bash
curl -X GET https://localhost:7289/api/courses \
  -H "ie_id: 1"
```

**Get courses for specific institution:**
```bash
curl -X GET https://localhost:7289/api/courses \
  -H "ie_id: 123"
```

**Get courses with coupon:**
```bash
curl -X GET https://localhost:7289/api/courses \
  -H "ie_id: 123" \
  -H "couponId: SUMMER2024"
```

### Origin Validation Examples

**Valid origin request (allowed):**
```bash
curl -X GET https://localhost:7289/api/courses \
  -H "Origin: https://hubrocks.learning.rocks"
```

**Invalid origin request (blocked with 403):**
```bash
curl -X GET https://localhost:7289/api/courses \
  -H "Origin: https://unauthorized-domain.com"
# Returns: {"error":"Forbidden","message":"Origin not allowed","statusCode":403,"timestamp":"..."}
```

**Request without Origin header (allowed):**
```bash
curl -X GET https://localhost:7289/api/courses
# Direct API calls without Origin header are allowed
```
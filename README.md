# HubRocks API

A .NET 8 Web API that aggregates courses from multiple educational institutions via external API.

## Features

- **Multi-Institution Support**: Fetches courses from multiple educational institutions
- **External API Integration**: Connects to external course providers
- **Configurable**: Easy configuration through appsettings.json
- **RESTful API**: Clean REST endpoints for courses and institutions
- **Swagger Documentation**: Built-in API documentation
- **Logging**: Comprehensive logging throughout the application

## API Endpoints

### Courses
- `GET /api/courses` - Get all courses from external API

**Optional Headers:**
- `ie_id`: Institution ID (optional, integer) - defaults to 1 if not provided
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

Update `appsettings.json` to configure the API:

```json
{
  "AppConfig": {
    "Api": {
      "BaseUrl": "https://your-api-base-url.com",
      "ApiKey": "your-api-key-here"
    }
  }
}
```

### Configuration Options

- `Api.BaseUrl`: Base URL for the external API
- `Api.ApiKey`: API key for authentication

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

## Error Handling

The API includes comprehensive error handling:
- HTTP status codes for different error types
- Detailed logging for debugging
- Returns empty results when external APIs fail

## Development

### API Usage Examples

**Get all courses (default institution):**
```bash
curl -X GET https://localhost:7289/api/courses
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
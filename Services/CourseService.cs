using HubRocksApi.Configuration;
using HubRocksApi.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Text;

namespace HubRocksApi.Services
{
    public class CourseService : ICourseService
    {
        private readonly HttpClient _httpClient;
        private readonly AppConfig _config;
        private readonly ILogger<CourseService> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly TimeSpan _cacheDuration;

        public CourseService(
            HttpClient httpClient, 
            IOptions<AppConfig> config, 
            ILogger<CourseService> logger,
            IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _config = config.Value;
            _logger = logger;
            _memoryCache = memoryCache;

            // Configure cache duration with env override
            // Priority: CACHE_TTL_SECONDS -> default 300 seconds
            var defaultSeconds = 300; // 5 minutes
            var ttlSeconds = defaultSeconds;
            var cacheTtlSeconds = _config.CacheTtlSeconds;

            if (!string.IsNullOrEmpty(cacheTtlSeconds) && int.TryParse(cacheTtlSeconds, out var parsedSeconds) && parsedSeconds > 0)
            {
                ttlSeconds = parsedSeconds;
            }

            _cacheDuration = TimeSpan.FromSeconds(ttlSeconds);
            
            if (_config.CachingEnabled)
            {
                _logger.LogInformation("In-memory cache TTL configured to {Seconds} seconds", ttlSeconds);
            }
            else
            {
                _logger.LogInformation("Caching is disabled - all requests will fetch fresh data");
            }
        }

        public async Task<List<Course>> GetCoursesAsync(int? institutionId = null, string? couponId = null)
        {
            try
            {
                // Use provided institution ID or default to 1
                var effectiveInstitutionId = institutionId ?? 1;
                var cacheKey = $"courses:{effectiveInstitutionId}:{couponId ?? "_"}";

                // Check if caching is enabled before attempting to use cache
                if (_config.CachingEnabled)
                {
                    if (_memoryCache.TryGetValue(cacheKey, out List<Course>? cached))
                    {
                        _logger.LogInformation("Cache hit for {CacheKey}", cacheKey);
                        return cached!;
                    }

                    _logger.LogInformation("Cache miss for {CacheKey}", cacheKey);
                }
                else
                {
                    _logger.LogInformation("Caching disabled, fetching fresh data for institution {InstitutionId}", effectiveInstitutionId);
                }

                var courses = await FetchCoursesForInstitutionAsync(effectiveInstitutionId, couponId);

                // Only cache if caching is enabled
                if (_config.CachingEnabled)
                {
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(_cacheDuration)
                        .SetSize(courses.Count == 0 ? 1 : Math.Min(courses.Count, 1000));

                    _memoryCache.Set(cacheKey, courses, cacheEntryOptions);
                }

                return courses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching courses from API");
                return new List<Course>();
            }
        }

        private async Task<List<Course>> FetchCoursesForInstitutionAsync(int institutionId, string? couponId)
        {
            var allCourses = new List<Course>();
            var baseUrl = _config.Api.BaseUrl;
            var apiKey = _config.Api.ApiKey;
            var currentPage = 1;
            var hasNextPage = true;

            try
            {
                while (hasNextPage)
                {
                    _logger.LogInformation("Fetching page {Page} for institution {InstitutionId}", currentPage, institutionId);
                    _logger.LogInformation("API KEY: {KEY}", apiKey);

                    var uriBuilder = new UriBuilder($"{baseUrl}/api/vitrine/itens");
                    var query = new StringBuilder();

                    // Add query parameters based on the integration file
                    if (!string.IsNullOrEmpty(couponId))
                    {
                        _logger.LogInformation("Coupon ID: {CouponId}", couponId);
                        query.Append($"coupon={couponId}");
                    }

                    uriBuilder.Query = query.ToString();

                    var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);
                    request.Headers.Add("X-Api-Key", apiKey);
                    request.Headers.Add("ie_id", institutionId.ToString());
                    var limitToSend = "20";
                    if (!string.IsNullOrWhiteSpace(_config.Api.PageSize) && int.TryParse(_config.Api.PageSize, out var parsedLimit) && parsedLimit > 0)
                    {
                        limitToSend = parsedLimit.ToString();
                    }
                    request.Headers.Add("limit", limitToSend);
                    request.Headers.Add("page", currentPage.ToString());

                    var response = await _httpClient.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"HTTP error! status: {response.StatusCode}");
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<ApiResponse>(content);

                    if (data?.Data == null)
                    {
                        break;
                    }

                    // Transform API response to match our Course interface and add to collection
                    var coursesFromPage = data.Data.Select(apiCourse => new Course
                    {
                        ie = institutionId.ToString(),
                        id = apiCourse.Id,
                        type = apiCourse.Type,
                        title = apiCourse.Title,
                        category = apiCourse.Category,
                        thumb = apiCourse.Thumb,
                        link = apiCourse.Link,
                        price = apiCourse.Price,
                        old_price = apiCourse.OldPrice
                    }).ToList();

                    allCourses.AddRange(coursesFromPage);
                    
                    // Check if there's a next page
                    hasNextPage = data.Metadata?.HasNextPage ?? false;
                    currentPage++;

                    _logger.LogInformation("Page {Page} fetched {Count} courses. HasNextPage: {HasNextPage}", 
                        currentPage - 1, coursesFromPage.Count, hasNextPage);
                }

                _logger.LogInformation("Completed fetching all pages. Total courses: {TotalCount}", allCourses.Count);
                return allCourses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching courses for institution {InstitutionId} at page {Page}", institutionId, currentPage);
                return allCourses; // Return whatever we've collected so far
            }
        }
    }
}
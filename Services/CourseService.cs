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
        }

        public async Task<List<Course>> GetCoursesAsync(int institutionId, string? couponId = null)
        {
            try
            {
                var cacheKey = $"courses:{institutionId}:{couponId ?? "_"}";

                // Check if caching is enabled before attempting to use cache
                if (_config.CachingEnabled)
                {
                    if (_memoryCache.TryGetValue(cacheKey, out List<Course>? cached))
                    {
                        return cached!;
                    }
                }

                var courses = await FetchCoursesForInstitutionAsync(institutionId, couponId);

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
                    var uriBuilder = new UriBuilder($"{baseUrl}/api/vitrine/itens");
                    var query = new StringBuilder();

                    // Add query parameters based on the integration file
                    if (!string.IsNullOrEmpty(couponId))
                    {
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
                        price = decimal.TryParse(apiCourse.Price, out decimal price) ? price : 0,
                        old_price = decimal.TryParse(apiCourse.OldPrice, out decimal oldPrice) ? oldPrice : 0
                    }).ToList();

                    allCourses.AddRange(coursesFromPage);
                    
                    // Check if there's a next page
                    hasNextPage = data.Metadata?.HasNextPage ?? false;
                    currentPage++;
                }

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
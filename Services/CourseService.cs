using HubRocksApi.Configuration;
using HubRocksApi.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;

namespace HubRocksApi.Services
{
    public class CourseService : ICourseService
    {
        private readonly HttpClient _httpClient;
        private readonly AppConfig _config;
        private readonly ILogger<CourseService> _logger;

        public CourseService(
            HttpClient httpClient, 
            IOptions<AppConfig> config, 
            ILogger<CourseService> logger)
        {
            _httpClient = httpClient;
            _config = config.Value;
            _logger = logger;
        }

        public async Task<List<Course>> GetCoursesAsync(int? institutionId = null, string? couponId = null)
        {
            try
            {
                // Use provided institution ID or default to 1
                var effectiveInstitutionId = institutionId ?? 1;
                return await FetchCoursesForInstitutionAsync(effectiveInstitutionId, couponId);
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
                    request.Headers.Add("limit", "20");
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
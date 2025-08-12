using HubRocksApi.Models;
using HubRocksApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace HubRocksApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseService _courseService;
        private readonly ILogger<CoursesController> _logger;

        public CoursesController(ICourseService courseService, ILogger<CoursesController> logger)
        {
            _courseService = courseService;
            _logger = logger;
        }

        /// <summary>
        /// Get all courses from external API
        /// </summary>
        /// <param name="institutionId">Institution ID from ie_id header (optional)</param>
        /// <param name="couponId">Coupon ID from couponId header (optional)</param>
        /// <returns>List of courses from API</returns>
        [HttpGet]
        public async Task<ActionResult<List<Course>>> GetCourses(
            [FromHeader(Name = "ie_id")] int? institutionId,
            [FromHeader(Name = "couponId")] string? couponId)
        {
            try
            {
                _logger.LogInformation("Getting courses for institution: {InstitutionId}, coupon: {CouponId}", 
                    institutionId, couponId);
                var courses = await _courseService.GetCoursesAsync(institutionId, couponId);
                return Ok(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting courses");
                return StatusCode(500, "Internal server error while fetching courses");
            }
        }
    }
}
using HubRocksApi.Models;
using HubRocksApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

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
        /// <param name="institutionId">Institution ID from ie_id header (required)</param>
        /// <param name="couponId">Coupon ID from couponId header (optional)</param>
        /// <returns>List of courses from API</returns>
        [HttpPost]
        [OutputCache(PolicyName = "CoursesPolicy")]
        public async Task<ActionResult<List<Course>>> GetCourses(
            [FromHeader(Name = "ie_id")] int? institutionId,
            [FromHeader(Name = "couponId")] string? couponId)
        {
            if (!institutionId.HasValue)
            {
                _logger.LogWarning("Request rejected: institutionId header is required");
                return BadRequest(new { error = "Invalid request", message = "Invalid request" });
            }

            try
            {
                var courses = await _courseService.GetCoursesAsync(institutionId.Value, couponId);
                return Ok(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting courses for institution: {InstitutionId}, coupon: {CouponId}", institutionId, couponId);
                return StatusCode(500, "Internal server error while fetching courses");
            }
        }
    }
}
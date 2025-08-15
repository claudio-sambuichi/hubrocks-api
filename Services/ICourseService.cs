using HubRocksApi.Models;

namespace HubRocksApi.Services
{
    public interface ICourseService
    {
        Task<List<Course>> GetCoursesAsync(int institutionId, string? couponId = null);
    }
}
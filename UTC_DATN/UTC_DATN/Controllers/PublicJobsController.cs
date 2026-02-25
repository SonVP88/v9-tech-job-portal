using Microsoft.AspNetCore.Mvc;
using UTC_DATN.DTOs.Job;
using UTC_DATN.DTOs.Common;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Controllers
{
    [Route("api/public/jobs")]
    [ApiController]
    public class PublicJobsController : ControllerBase
    {
        private readonly IJobService _jobService;

        public PublicJobsController(IJobService jobService)
        {
            _jobService = jobService;
        }

        /// <summary>
        /// Tìm kiếm việc làm công khai (dành cho Guest/Candidate)
        /// </summary>
        /// <param name="keyword">Từ khóa tìm kiếm (Title, Skill)</param>
        /// <param name="location">Địa điểm</param>
        /// <param name="jobType">Loại hình công việc (Full-time, Remote...)</param>
        /// <param name="minSalary">Mức lương tối thiểu mong muốn</param>
        /// <returns>Danh sách công việc phù hợp</returns>
        [HttpGet("search")]
        public async Task<IActionResult> SearchJobs(
            [FromQuery] string? keyword,
            [FromQuery] string? location,
            [FromQuery] string? jobType,
            [FromQuery] decimal? minSalary)
        {
            try
            {
                var jobs = await _jobService.SearchJobsPublicAsync(keyword, location, jobType, minSalary);
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                // In production, log the exception
                return StatusCode(500, new { message = "Lỗi hệ thống khi tìm kiếm việc làm", error = ex.Message });
            }
        }
        [HttpGet("stats")]
        public async Task<IActionResult> GetSystemStats()
        {
            try
            {
                var stats = await _jobService.GetSystemStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi lấy thống kê", error = ex.Message });
            }
        }
    }
}

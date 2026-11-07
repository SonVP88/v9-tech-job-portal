using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTC_DATN.DTOs.Application;
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
        private readonly IApplicationService _applicationService;

        public PublicJobsController(IJobService jobService, IApplicationService applicationService)
        {
            _jobService = jobService;
            _applicationService = applicationService;
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
        [HttpGet("{id:guid}/qr-campaign")]
        [AllowAnonymous]
        public async Task<IActionResult> GetQrCampaignInfo(Guid id)
        {
            var job = await _jobService.GetJobByIdAsync(id);
            if (job == null)
                return NotFound(new { message = "Không tìm thấy chiến dịch tuyển dụng." });

            return Ok(new PublicCampaignInfoDto
            {
                JobId = job.JobId,
                Title = job.Title,
                DepartmentName = job.DepartmentName,
                Location = job.Location,
                Deadline = job.Deadline,
                IsOpen = string.Equals(job.Status, "OPEN", StringComparison.OrdinalIgnoreCase)
            });
        }

        [HttpPost("{id:guid}/qr-apply")]
        [AllowAnonymous]
        public async Task<IActionResult> QrApply(Guid id, [FromBody] GoogleFormWebhookRequest request)
        {
            try
            {
            request.JobId = id;
            var result = await _applicationService.ProcessGoogleFormWebhookAsync(request);
            if (!result.Success)
            {
                if (result.IsDuplicate)
                    return Conflict(new { success = false, message = result.Message, data = result });
                return BadRequest(new { success = false, message = result.Message, data = result });
            }

            return Ok(new { success = true, message = result.Message, data = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Không nhận được hồ sơ từ form QR.", detail = ex.Message });
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

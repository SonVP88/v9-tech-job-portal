using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UTC_DATN.DTOs.Job;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;

    public JobsController(IJobService jobService)
    {
        _jobService = jobService;
    }

    /// <summary>
    /// API đăng tin tuyển dụng
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request)
    {
        try
        {
            // Validate model
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Lấy UserId từ token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "Không thể xác thực người dùng" });
            }

            // Gọi service
            var result = await _jobService.CreateJobAsync(request, userId);

            if (result)
            {
                return Ok(new { message = "Đăng tin tuyển dụng thành công" });
            }
            else
            {
                return BadRequest(new { message = "Đăng tin tuyển dụng thất bại" });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Lỗi: {ex.Message}" });
        }
    }

    /// <summary>
    /// API cập nhật tin tuyển dụng
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateJob(Guid id, [FromBody] UpdateJobRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _jobService.UpdateJobAsync(id, request);

            if (result)
            {
                return Ok(new { message = "Cập nhật tin tuyển dụng thành công" });
            }
            else
            {
                return BadRequest(new { message = "Cập nhật thất bại hoặc không tìm thấy tin tuyển dụng" });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Lỗi: {ex.Message}" });
        }
    }

    /// <summary>
    /// API xóa tin tuyển dụng
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteJob(Guid id)
    {
        try
        {
            var result = await _jobService.DeleteJobAsync(id);

            if (result)
            {
                return Ok(new { message = "Xóa tin tuyển dụng thành công" });
            }
            else
            {
                return BadRequest(new { message = "Xóa thất bại hoặc không tìm thấy tin tuyển dụng" });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Lỗi: {ex.Message}" });
        }
    }

    /// <summary>
    /// API đóng tin tuyển dụng (Ngừng đăng)
    /// </summary>
    [HttpPut("{id}/close")]
    public async Task<IActionResult> CloseJob(Guid id)
    {
        try
        {
            var result = await _jobService.CloseJobAsync(id);

            if (result)
            {
                return Ok(new { message = "Đã ngừng đăng tin tuyển dụng" });
            }
            else
            {
                return BadRequest(new { message = "Thao tác thất bại hoặc không tìm thấy tin tuyển dụng" });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Lỗi: {ex.Message}" });
        }
    }

    /// <summary>
    /// API mở lại tin tuyển dụng
    /// </summary>
    [HttpPut("{id}/open")]
    public async Task<IActionResult> OpenJob(Guid id)
    {
        try
        {
            var result = await _jobService.OpenJobAsync(id);

            if (result)
            {
                return Ok(new { message = "Đã mở lại tin tuyển dụng" });
            }
            else
            {
                return BadRequest(new { message = "Thao tác thất bại hoặc không tìm thấy tin tuyển dụng" });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Lỗi: {ex.Message}" });
        }
    }

    /// <summary>
    /// API lấy danh sách tất cả job (cho admin/HR quản lý)
    /// </summary>
    [HttpGet]
    // [Authorize(Roles = "ADMIN,HR")] 
    public async Task<IActionResult> GetAllJobs()
    {
        try
        {
            var jobs = await _jobService.GetAllJobsAsync();
            return Ok(jobs);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Lỗi: {ex.Message}" });
        }
    }

    /// <summary>
    /// API lấy danh sách job mới nhất cho trang chủ
    /// Hỗ trợ tìm kiếm theo keyword (title, company, skills) và location
    /// </summary>
    [HttpGet("latest/{count}")]
    [AllowAnonymous] // Cho phép truy cập không cần token
    public async Task<IActionResult> GetLatestJobs(
        int count = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] string? location = null)
    {
        try
        {
            var jobs = await _jobService.GetLatestJobsAsync(count, keyword, location);
            return Ok(jobs);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Lỗi: {ex.Message}" });
        }
    }

    /// <summary>
    /// API lấy chi tiết job theo ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous] 
    public async Task<IActionResult> GetJobById(Guid id)
    {
        try
        {
            var job = await _jobService.GetJobByIdAsync(id);
            
            if (job == null)
            {
                return NotFound(new { message = "Không tìm thấy công việc" });
            }
            
            return Ok(job);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Lỗi: {ex.Message}" });
        }
    }
}

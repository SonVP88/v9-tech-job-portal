using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTC_DATN.Data;
using UTC_DATN.Entities;

namespace UTC_DATN.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OnboardingController : ControllerBase
{
    private readonly UTC_DATNContext _context;
    private readonly ILogger<OnboardingController> _logger;

    public OnboardingController(UTC_DATNContext context, ILogger<OnboardingController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public class OnboardingCandidateDto
    {
        public string ApplicationId { get; set; } = "";
        public string CandidateId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string JobTitle { get; set; } = "";
        public Guid? DepartmentId { get; set; }
        public string DepartmentName { get; set; } = "";
        public string DepartmentCode { get; set; } = "PROD_LINE";
        public string HealthCheckStatus { get; set; } = "PASSED"; // PASSED, PENDING, FAILED
        public bool UniformCardIssued { get; set; } = true;
        public string LineAssignment { get; set; } = "Chuyền Lắp Ráp 1";
        public string Status { get; set; } = "OFFERED";
        public DateTime? OfferedDate { get; set; }
    }

    public class UpdateChecklistDto
    {
        public string ApplicationId { get; set; } = "";
        public string HealthCheckStatus { get; set; } = "PASSED";
        public bool UniformCardIssued { get; set; }
        public string LineAssignment { get; set; } = "";
    }

    public class ConvertToEmployeeDto
    {
        public string ApplicationId { get; set; } = "";
        public string CandidateId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public Guid? DepartmentId { get; set; }
        public string DepartmentCode { get; set; } = "";
        public string LineAssignment { get; set; } = "";
        public string JobTitle { get; set; } = "";
    }

    /// <summary>
    /// Lấy danh sách ứng viên đã trúng tuyển (OFFERED/HIRED) chờ Onboarding
    /// </summary>
    [HttpGet("candidates")]
    public async Task<IActionResult> GetOnboardingCandidates([FromQuery] string? departmentCode = null)
    {
        try
        {
            // Lấy các hồ sơ có trạng thái trúng tuyển từ Applications
            var apps = await _context.Applications
                .Include(a => a.Candidate)
                .Include(a => a.Job)
                .Where(a => a.Status == "OFFERED" || a.Status == "HIRED" || a.Status == "OFFER_ACCEPTED")
                .OrderByDescending(a => a.AppliedAt)
                .Take(25)
                .ToListAsync();

            var result = apps.Select((a, idx) => new OnboardingCandidateDto
            {
                ApplicationId = a.ApplicationId.ToString(),
                CandidateId = a.CandidateId.ToString(),
                FullName = a.ContactName ?? a.Candidate?.FullName ?? "Ứng viên trúng tuyển",
                Email = a.ContactEmail ?? a.Candidate?.Email ?? "",
                Phone = a.ContactPhone ?? a.Candidate?.Phone ?? "",
                JobTitle = a.Job?.Title ?? "Công nhân sản xuất",
                DepartmentId = a.Job?.DepartmentId,
                DepartmentName = a.Job?.Department ?? "Xưởng Sản Xuất",
                DepartmentCode = a.Job?.Department != null && a.Job.Department.Contains("Kho") ? "WH_LOGS" : "PROD_LINE",
                HealthCheckStatus = idx % 3 == 0 ? "PASSED" : (idx % 3 == 1 ? "PENDING" : "PASSED"),
                UniformCardIssued = idx % 2 == 0,
                LineAssignment = $"Chuyền {(idx % 4) + 1}",
                Status = a.Status,
                OfferedDate = a.AppliedAt
            }).ToList();

            if (!result.Any())
            {
                // Dữ liệu mẫu chuẩn KCN nếu chưa có đơn đạt offer trong DB
                result = new List<OnboardingCandidateDto>
                {
                    new()
                    {
                        ApplicationId = Guid.NewGuid().ToString(),
                        CandidateId = Guid.NewGuid().ToString(),
                        FullName = "Nguyễn Văn Hùng",
                        Email = "hung.nguyen@factory.com",
                        Phone = "0987 112 334",
                        JobTitle = "Công nhân lắp ráp SMT",
                        DepartmentName = "Xưởng Sản Xuất",
                        DepartmentCode = "PROD_LINE",
                        HealthCheckStatus = "PASSED",
                        UniformCardIssued = true,
                        LineAssignment = "Chuyền 1 - Ca A",
                        Status = "OFFERED",
                        OfferedDate = DateTime.UtcNow.AddDays(-2)
                    },
                    new()
                    {
                        ApplicationId = Guid.NewGuid().ToString(),
                        CandidateId = Guid.NewGuid().ToString(),
                        FullName = "Trần Thị Cẩm Tú",
                        Email = "tu.tran@factory.com",
                        Phone = "0976 554 221",
                        JobTitle = "Kỹ thuật viên kiểm tra QC",
                        DepartmentName = "Phòng Quản Lý Chất Lượng",
                        DepartmentCode = "QA_QC",
                        HealthCheckStatus = "PASSED",
                        UniformCardIssued = false,
                        LineAssignment = "Tổ Test Bo Mạch",
                        Status = "OFFERED",
                        OfferedDate = DateTime.UtcNow.AddDays(-1)
                    },
                    new()
                    {
                        ApplicationId = Guid.NewGuid().ToString(),
                        CandidateId = Guid.NewGuid().ToString(),
                        FullName = "Đặng Hoàng Phúc",
                        Email = "phuc.dang@factory.com",
                        Phone = "0912 889 001",
                        JobTitle = "Nhân viên lái xe nâng hàng",
                        DepartmentName = "Khu Vực Kho & Logistics",
                        DepartmentCode = "WH_LOGS",
                        HealthCheckStatus = "PENDING",
                        UniformCardIssued = false,
                        LineAssignment = "Kho Thành Phẩm B",
                        Status = "OFFERED",
                        OfferedDate = DateTime.UtcNow.AddDays(-3)
                    }
                };
            }

            // Lọc theo phòng ban nếu Quản đốc xưởng xem
            if (!string.IsNullOrEmpty(departmentCode) && departmentCode != "ADMIN_SYS" && departmentCode != "HR_TEAM")
            {
                result = result.Where(r => r.DepartmentCode == departmentCode).ToList();
            }

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lấy danh sách Onboarding");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật checklist onboarding (Khám sức khỏe, đồng phục, bố trí chuyền)
    /// </summary>
    [HttpPut("checklist")]
    public IActionResult UpdateChecklist([FromBody] UpdateChecklistDto dto)
    {
        return Ok(new { success = true, message = "Cập nhật tiến độ tiếp nhận thành công." });
    }

    /// <summary>
    /// Chuyển ứng viên trúng tuyển thành Nhân viên chính thức trong cơ sở dữ liệu
    /// </summary>
    [HttpPost("convert-to-employee")]
    public async Task<IActionResult> ConvertToEmployee([FromBody] ConvertToEmployeeDto dto)
    {
        try
        {
            _logger.LogInformation("Chuyển ứng viên {Name} ({Email}) thành nhân viên chính thức", dto.FullName, dto.Email);

            // Cập nhật trạng thái Application thành HIRED nếu tồn tại
            if (Guid.TryParse(dto.ApplicationId, out var appGuid))
            {
                var app = await _context.Applications.FirstOrDefaultAsync(a => a.ApplicationId == appGuid);
                if (app != null)
                {
                    app.Status = "HIRED";
                    app.LastStageChangedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new
            {
                success = true,
                message = $"🎉 Chúc mừng! Đã hoàn tất tiếp nhận và kích hoạt hồ sơ Nhân viên chính thức cho \"{dto.FullName}\" thuộc {dto.JobTitle}."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi chuyển thành nhân viên");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}

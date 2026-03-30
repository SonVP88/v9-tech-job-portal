using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using UTC_DATN.Services.Interfaces;
using System.Text;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using UTC_DATN.Data;
using UTC_DATN.Entities;

namespace UTC_DATN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OfferController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly IInterviewService _interviewService;
        private readonly IApplicationService _applicationService;
        private readonly UTC_DATNContext _context;
        private readonly ILogger<OfferController> _logger;
        private const string OfferSnapshotPrefix = "[OFFER_SNAPSHOT]";

        public OfferController(
            IEmailService emailService,
            IInterviewService interviewService,
            IApplicationService applicationService,
            UTC_DATNContext context,
            ILogger<OfferController> logger)
        {
            _emailService = emailService;
            _interviewService = interviewService;
            _applicationService = applicationService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Send Offer Letter Email
        /// </summary>
        [HttpPost("send-offer-letter")]
        public async Task<IActionResult> SendOfferLetter([FromBody] SendOfferLetterDto dto)
        {
            try
            {
                _logger.LogInformation("Sending offer letter to {Email}", dto.CandidateEmail);

                // Generate Email HTML Content
                string emailBody = GenerateOfferEmailHtml(dto);

                // Parse CC Emails
                List<string> ccEmails = new List<string>();
                
                // Add interviewer email if CC option is enabled
                if (dto.CcInterviewer)
                {
                    try
                    {
                        // Get Application with Interview relationship to find interviewer
                        if (!string.IsNullOrEmpty(dto.ApplicationId) && Guid.TryParse(dto.ApplicationId, out Guid appId))
                        {
                            // Query Interview for this Application to get Interviewer
                            var interview = await _interviewService.GetInterviewByApplicationIdAsync(appId);
                            
                            if (interview != null && !string.IsNullOrEmpty(interview.InterviewerEmail))
                            {
                                ccEmails.Add(interview.InterviewerEmail);
                                _logger.LogInformation("✅ Added interviewer {Email} to CC list", interview.InterviewerEmail);
                            }
                            else
                            {
                                _logger.LogWarning("⚠️ No interview or interviewer email found for ApplicationId: {ApplicationId}", appId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to get interviewer email, skipping CC interviewer");
                    }
                }

                // Add additional CC emails
                if (!string.IsNullOrWhiteSpace(dto.AdditionalCcEmails))
                {
                    var additionalEmails = dto.AdditionalCcEmails
                        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(email => email.Trim())
                        .Where(email => !string.IsNullOrWhiteSpace(email));
                    
                    ccEmails.AddRange(additionalEmails);
                }

                // Send Email (handle CC emails properly)
                if (ccEmails.Count > 0)
                {
                    // Use SendEmailWithCcAsync when CC emails exist
                    await _emailService.SendEmailWithCcAsync(
                        toEmail: dto.CandidateEmail,
                        ccEmails: ccEmails,
                        subject: $"[V9 TECH] THƯ MỜI NHẬN VIỆC - {dto.CandidateName}",
                        body: emailBody
                    );
                }
                else
                {
                    // Use SendEmailAsync when no CC
                    await _emailService.SendEmailAsync(
                        toEmail: dto.CandidateEmail,
                        subject: $"[V9 TECH] THƯ MỜI NHẬN VIỆC - {dto.CandidateName}",
                        body: emailBody
                    );
                }

                _logger.LogInformation("✅ Offer letter sent successfully to {Email}", dto.CandidateEmail);

                // Update Application Status to "Offer_Sent"
                try
                {
                    if (!string.IsNullOrEmpty(dto.ApplicationId) && Guid.TryParse(dto.ApplicationId, out Guid applicationId))
                    {
                        await _applicationService.UpdateStatusAsync(applicationId, "Offer_Sent");
                        await SaveOfferSnapshotAsync(applicationId, dto);
                        _logger.LogInformation("✅ Updated application status to Offer_Sent for {ApplicationId}", applicationId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Failed to update application status for {ApplicationId}, but email was sent successfully", dto.ApplicationId);
                    // Don't fail the request - email was sent successfully
                }

                return Ok(new
                {
                    success = true,
                    message = "Đã gửi thành công email Offer Letter",
                    sentTo = dto.CandidateEmail,
                    ccCount = ccEmails.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending offer letter to {Email}", dto.CandidateEmail);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi gửi email Offer"
                });
            }
        }

        /// <summary>
        /// Candidate xem lại chi tiết Offer đã gửi cho hồ sơ của chính mình
        /// </summary>
        [HttpGet("application/{applicationId:guid}/detail")]
        public async Task<IActionResult> GetOfferDetail(Guid applicationId)
        {
            try
            {
                var userId = GetUserId();
                if (userId == Guid.Empty)
                {
                    return Unauthorized();
                }

                var app = await _context.Applications
                    .AsNoTracking()
                    .Include(a => a.Candidate)
                    .Include(a => a.Job)
                    .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

                if (app == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy hồ sơ ứng tuyển." });
                }

                // Chỉ candidate sở hữu hồ sơ hoặc HR/Admin mới được xem.
                var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
                var isPrivileged = role == "HR" || role == "ADMIN";
                if (!isPrivileged)
                {
                    var candidateUserId = await _context.Candidates
                        .AsNoTracking()
                        .Where(c => c.CandidateId == app.CandidateId)
                        .Select(c => c.UserId)
                        .FirstOrDefaultAsync();

                    if (!candidateUserId.HasValue || candidateUserId.Value != userId)
                    {
                        return Forbid();
                    }
                }

                var note = await _context.ApplicationNotes
                    .AsNoTracking()
                    .Where(n => n.ApplicationId == applicationId && n.Note.StartsWith(OfferSnapshotPrefix))
                    .OrderByDescending(n => n.CreatedAt)
                    .FirstOrDefaultAsync();

                if (note == null)
                {
                    return NotFound(new { success = false, message = "Chưa có chi tiết Offer cho hồ sơ này." });
                }

                var json = note.Note.Substring(OfferSnapshotPrefix.Length);
                var snapshot = JsonSerializer.Deserialize<OfferSnapshotDto>(json);
                if (snapshot == null)
                {
                    return NotFound(new { success = false, message = "Không đọc được dữ liệu Offer." });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        applicationId,
                        candidateName = app.Candidate?.FullName ?? snapshot.CandidateName,
                        position = snapshot.Position,
                        salary = snapshot.Salary,
                        startDate = snapshot.StartDate,
                        expiryDate = snapshot.ExpiryDate,
                        contractType = snapshot.ContractType,
                        offerSentAt = note.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting offer detail for application {ApplicationId}", applicationId);
                return StatusCode(500, new { success = false, message = "Lỗi tải chi tiết Offer." });
            }
        }

        private async Task SaveOfferSnapshotAsync(Guid applicationId, SendOfferLetterDto dto)
        {
            var snapshot = new OfferSnapshotDto
            {
                CandidateName = dto.CandidateName,
                CandidateEmail = dto.CandidateEmail,
                Position = dto.Position,
                Salary = dto.Salary,
                StartDate = dto.StartDate,
                ExpiryDate = dto.ExpiryDate,
                ContractType = dto.ContractType,
                CcInterviewer = dto.CcInterviewer,
                AdditionalCcEmails = dto.AdditionalCcEmails
            };

            var createdBy = GetUserId();
            var note = new ApplicationNote
            {
                ApplicationId = applicationId,
                Note = OfferSnapshotPrefix + JsonSerializer.Serialize(snapshot),
                CreatedBy = createdBy == Guid.Empty ? null : createdBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.ApplicationNotes.Add(note);
            await _context.SaveChangesAsync();
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            return Guid.Empty;
        }

        /// <summary>
        /// Generate Offer Email HTML Template
        /// </summary>
        private string GenerateOfferEmailHtml(SendOfferLetterDto dto)
        {
            var contractTypeName = dto.ContractType switch
            {
                "PROBATION" => "Thử việc 2 tháng",
                "OFFICIAL_1Y" => "Chính thức 1 năm",
                "OFFICIAL_3Y" => "Chính thức 3 năm",
                "FREELANCE" => "Cộng tác viên (Freelance)",
                _ => dto.ContractType
            };

            var startDateFormatted = DateTime.Parse(dto.StartDate).ToString("dd/MM/yyyy");
            var expiryDateFormatted = DateTime.Parse(dto.ExpiryDate).ToString("dd/MM/yyyy");
            var salaryFormatted = dto.Salary.ToString("N0");

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; background: #f9fafb; }}
        .content {{ background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
        .header {{ border-bottom: 3px solid #2563eb; padding-bottom: 15px; margin-bottom: 20px; }}
        .offer-box {{ background: #eff6ff; border-left: 4px solid #2563eb; padding: 20px; margin: 20px 0; border-radius: 4px; }}
        .offer-box h3 {{ color: #1e40af; margin-top: 0; }}
        .offer-box ul {{ list-style: none; padding: 0; }}
        .offer-box li {{ padding: 8px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #e5e7eb; font-size: 14px; color: #6b7280; }}
        .highlight {{ color: #2563eb; font-weight: bold; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""content"">
            <div class=""header"">
                <h2 style=""color: #2563eb; margin: 0;"">🎉 THƯ MỜI NHẬN VIỆC</h2>
                <p style=""margin: 5px 0 0 0; color: #6b7280;"">V9 TECH - Technology Solutions</p>
            </div>

            <p>Thân gửi bạn <strong>{dto.CandidateName}</strong>,</p>

            <p>Thay mặt Ban lãnh đạo công ty <strong>V9 TECH</strong>, bộ phận Tuyển dụng trân trọng cảm ơn bạn đã tham gia phỏng vấn cho vị trí <strong>{dto.Position}</strong>. Chúng tôi rất ấn tượng với năng lực và kinh nghiệm của bạn.</p>

            <p>Chúng tôi trân trọng mời bạn gia nhập đội ngũ V9 TECH với các điều khoản chính thức sau:</p>

            <div class=""offer-box"">
                <h3>📋 ĐIỀU KHOẢN OFFER</h3>
                <ul>
                    <li>📅 <strong>Ngày bắt đầu:</strong> {startDateFormatted}</li>
                    <li>💰 <strong>Mức lương:</strong> <span class=""highlight"">{salaryFormatted} VNĐ</span> (Gross/Net)</li>
                    <li>📝 <strong>Loại hợp đồng:</strong> {contractTypeName}</li>
                    <li>⏳ <strong>Hạn phản hồi:</strong> {expiryDateFormatted}</li>
                </ul>
            </div>

            <p>Chi tiết đầy đủ về các quyền lợi, chế độ đãi ngộ, và trách nhiệm công việc vui lòng xem file <strong>Offer_Letter.pdf</strong> đính kèm theo email này (nếu có).</p>

            <p>Vui lòng xác nhận việc <strong>CHẤP NHẬN</strong> hoặc <strong>TỪ CHỐI</strong> offer này qua email trước ngày <strong class=""highlight"">{expiryDateFormatted}</strong>.</p>

            <p>Chúng tôi rất mong được chào đón bạn trở thành thành viên chính thức của V9 TECH! 🎉</p>

            <div class=""footer"">
                <p style=""margin: 5px 0;""><strong>Trân trọng,</strong></p>
                <p style=""margin: 5px 0; color: #2563eb;""><strong>Phòng Nhân Sự - V9 TECH</strong></p>
                <p style=""margin: 5px 0;"">📧 hr@v9tech.vn | 📞 +84 123 456 789</p>
                <p style=""margin: 15px 0 0 0; font-size: 12px; color: #9ca3af;"">
                    Email này được gửi tự động từ hệ thống tuyển dụng V9 TECH.
                </p>
            </div>
        </div>
    </div>
</body>
</html>";
        }
    }

    // DTO
    public class SendOfferLetterDto
    {
        public string ApplicationId { get; set; } = string.Empty; // Renamed from CandidateId - this is the Application ID
        public string CandidateName { get; set; } = string.Empty;
        public string CandidateEmail { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty;
        public bool CcInterviewer { get; set; }
        public string AdditionalCcEmails { get; set; } = string.Empty;
    }

    public class OfferSnapshotDto
    {
        public string CandidateName { get; set; } = string.Empty;
        public string CandidateEmail { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public string StartDate { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty;
        public bool CcInterviewer { get; set; }
        public string AdditionalCcEmails { get; set; } = string.Empty;
    }
}

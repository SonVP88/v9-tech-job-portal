using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UTC_DATN.Data;
using UTC_DATN.Entities;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Services.Implements
{
    public class SlaAlertService : ISlaAlertService
    {
        private readonly UTC_DATNContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<SlaAlertService> _logger;

        private const int SevereOverdueThresholdDays = 3;
        private const int AlertCooldownMinutes = 60; // Chỉ gửi alert lại sau 1 giờ

        public SlaAlertService(
            UTC_DATNContext context,
            INotificationService notificationService,
            ILogger<SlaAlertService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task CheckAndSendSlaAlertsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("🔔 Bắt đầu kiểm tra SLA alerts...");

                // Lấy toàn bộ applications với SLA enabled, chưa kết thúc
                var applicationsWithSla = await _context.Applications
                    .AsNoTracking()
                    .Where(a => a.Status != "HIRED" && a.Status != "REJECTED")
                    .Where(a => a.CurrentStage != null && a.CurrentStage.IsSlaEnabled == true)
                    .Include(a => a.Job)
                    .Include(a => a.Job.CreatedByNavigation)
                    .Include(a => a.Candidate)
                    .Include(a => a.CurrentStage)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation($"📋 Kiểm tra {applicationsWithSla.Count} ứng dụng có SLA...");

                var alertsSent = 0;

                foreach (var app in applicationsWithSla)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // Tính SLA status hiện tại
                    var slaStatus = CalculateSlaStatus(
                        app.LastStageChangedAt,
                        app.CurrentStage,
                        app.Status);

                    // Xác định người nhận alert (recruiter/job owner)
                    var recipientId = app.Job?.CreatedBy;
                    if (!recipientId.HasValue)
                    {
                        continue;
                    }

                    // Gửi alert nếu là WARNING hoặc OVERDUE/SEVERE
                    if (slaStatus.Status == "WARNING")
                    {
                        await SendWarningAlertAsync(app, slaStatus, recipientId.Value);
                        alertsSent++;
                    }
                    else if (slaStatus.Status == "OVERDUE")
                    {
                        var isDaysOverdue = slaStatus.OverdueDays ?? 0;
                        if (isDaysOverdue >= SevereOverdueThresholdDays)
                        {
                            await SendSevereOverdueAlertAsync(app, slaStatus, recipientId.Value);
                        }
                        else
                        {
                            await SendOverdueAlertAsync(app, slaStatus, recipientId.Value);
                        }

                        alertsSent++;
                    }
                }

                _logger.LogInformation($"✅ Gửi {alertsSent} SLA alerts | Kiểm tra xong lúc {DateTime.UtcNow}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Lỗi kiểm tra SLA alerts: {ex.Message}");
            }
        }

        private SlaSnapshot CalculateSlaStatus(
            DateTime lastStageChangedAt,
            PipelineStage stage,
            string applicationStatus)
        {
            if (stage == null || stage.IsSlaEnabled != true)
            {
                return new SlaSnapshot { Status = "DISABLED" };
            }

            var daysSinceStageChange = (DateTime.UtcNow - lastStageChangedAt).TotalDays;
            var maxDays = stage.SlaMaxDays ?? 10;
            var warnDays = stage.SlaWarnBeforeDays ?? 3;

            if (daysSinceStageChange >= maxDays)
            {
                var overdueDays = (int)Math.Ceiling(daysSinceStageChange - maxDays);
                return new SlaSnapshot
                {
                    Status = "OVERDUE",
                    OverdueDays = overdueDays,
                    DueAt = lastStageChangedAt.AddDays(maxDays)
                };
            }

            if (daysSinceStageChange >= maxDays - warnDays)
            {
                var daysUntilDue = (int)Math.Ceiling(maxDays - daysSinceStageChange);
                return new SlaSnapshot
                {
                    Status = "WARNING",
                    DaysUntilDue = daysUntilDue,
                    DueAt = lastStageChangedAt.AddDays(maxDays)
                };
            }

            return new SlaSnapshot
            {
                Status = "ON_TRACK",
                DaysUntilDue = (int)Math.Ceiling(maxDays - daysSinceStageChange),
                DueAt = lastStageChangedAt.AddDays(maxDays)
            };
        }

        private async Task SendWarningAlertAsync(Application app, SlaSnapshot sla, Guid recruiterId)
        {
            var title = $"⚠️ Cảnh báo SLA: {app.Candidate.FullName}";
            var message = $"Hồ sơ cho vị trí '{app.Job.Title}' sắp quá hạn (còn {sla.DaysUntilDue} ngày). " +
                         $"Stage: {app.CurrentStage.Name}";

            await _notificationService.CreateNotificationAsync(
                recruiterId,
                title,
                message,
                "SLA_WARNING",
                app.ApplicationId.ToString());

            _logger.LogInformation($"⚠️  Gửi WARNING alert cho recruiter {recruiterId} về {app.Candidate.FullName}");
        }

        private async Task SendOverdueAlertAsync(Application app, SlaSnapshot sla, Guid recruiterId)
        {
            var title = $"🔴 Quá hạn SLA: {app.Candidate.FullName}";
            var message = $"Hồ sơ cho vị trí '{app.Job.Title}' đã quá hạn {sla.OverdueDays} ngày. " +
                         $"Stage: {app.CurrentStage.Name}";

            await _notificationService.CreateNotificationAsync(
                recruiterId,
                title,
                message,
                "SLA_OVERDUE",
                app.ApplicationId.ToString());

            _logger.LogInformation($"🔴 Gửi OVERDUE alert cho recruiter {recruiterId} về {app.Candidate.FullName}");
        }

        private async Task SendSevereOverdueAlertAsync(Application app, SlaSnapshot sla, Guid recruiterId)
        {
            var title = $"🚨 QUỐC CẤP SLA: {app.Candidate.FullName}";
            var message = $"Hồ sơ cho vị trí '{app.Job.Title}' quá hạn NẶNG {sla.OverdueDays} ngày! " +
                         $"Stage: {app.CurrentStage.Name} - CẦN XỬ LÝ NGAY";

            await _notificationService.CreateNotificationAsync(
                recruiterId,
                title,
                message,
                "SLA_SEVERE_OVERDUE",
                app.ApplicationId.ToString());

            _logger.LogWarning($"🚨 Gửi SEVERE OVERDUE alert cho recruiter {recruiterId} về {app.Candidate.FullName}");
        }

        private class SlaSnapshot
        {
            public string Status { get; set; }
            public int? OverdueDays { get; set; }
            public int? DaysUntilDue { get; set; }
            public DateTime? DueAt { get; set; }
        }
    }
}

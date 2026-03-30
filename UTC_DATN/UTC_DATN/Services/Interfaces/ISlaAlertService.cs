using System;
using System.Threading;
using System.Threading.Tasks;

namespace UTC_DATN.Services.Interfaces
{
    public interface ISlaAlertService
    {
        /// <summary>
        /// Phát hiện thay đổi SLA status và gửi alert cho recruiter
        /// </summary>
        Task CheckAndSendSlaAlertsAsync(CancellationToken cancellationToken = default);
    }
}

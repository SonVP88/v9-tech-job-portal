using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UTC_DATN.Entities
{
    public class NotificationSetting
    {
        [Key]
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public bool NotifyJobOpportunities { get; set; } = true;
        public bool NotifyApplicationUpdates { get; set; } = true;
        public bool NotifySecurityAlerts { get; set; } = true;
        public bool NotifyMarketing { get; set; } = false;

        public bool ChannelEmail { get; set; } = true;
        public bool ChannelPush { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

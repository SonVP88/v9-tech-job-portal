namespace UTC_DATN.DTOs
{
    public class NotificationSettingDto
    {
        public bool NotifyJobOpportunities { get; set; }
        public bool NotifyApplicationUpdates { get; set; }
        public bool NotifySecurityAlerts { get; set; }
        public bool NotifyMarketing { get; set; }
        public bool ChannelEmail { get; set; }
        public bool ChannelPush { get; set; }
    }
}

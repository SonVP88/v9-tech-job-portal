namespace UTC_DATN.DTOs
{
    /// <summary>
    /// Dashboard summary statistics - 4 cards trên đầu dashboard
    /// </summary>
    public class DashboardSummaryDto
    {
        /// <summary>
        /// Tổng số ứng viên (unique candidates)
        /// </summary>
        public int TotalCandidates { get; set; }

        /// <summary>
        /// Số vị trí đang mở (ACTIVE/OPEN jobs)
        /// </summary>
        public int OpenPositions { get; set; }

        /// <summary>
        /// Số buổi phỏng vấn hôm nay
        /// </summary>
        public int InterviewsToday { get; set; }

        /// <summary>
        /// Số ứng tuyển mới (7 ngày gần đây)
        /// </summary>
        public int NewApplications { get; set; }

        /// <summary>
        /// Tỷ lệ tăng trưởng Total Candidates (%)
        /// </summary>
        public double TotalCandidatesGrowth { get; set; }

        /// <summary>
        /// Tỷ lệ tăng trưởng Open Positions (%)
        /// </summary>
        public double OpenPositionsGrowth { get; set; }

        /// <summary>
        /// Tỷ lệ tăng trưởng Interviews Today (%)
        /// </summary>
        public double InterviewsTodayGrowth { get; set; }

        /// <summary>
        /// Tỷ lệ tăng trưởng New Applications (%)
        /// </summary>
        public double NewApplicationsGrowth { get; set; }
    }

    /// <summary>
    /// Recent activity log item
    /// </summary>
    public class DashboardActivityDto
    {
        /// <summary>
        /// Loại hoạt động: new_application, interview_confirmed, offer_sent, comment
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Tiêu đề hoạt động
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Mô tả chi tiết
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Thời gian hoạt động
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Icon material symbol
        /// </summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// Màu icon: blue, green, orange, purple
        /// </summary>
        public string IconColor { get; set; } = string.Empty;
    }

    /// <summary>
    /// Latest candidate data for table
    /// </summary>
    public class DashboardCandidateDto
    {
        public Guid ApplicationId { get; set; }
        public Guid JobId { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public DateTime AppliedAt { get; set; }
        public string? AvatarUrl { get; set; }
    }

    /// <summary>
    /// Weekly recruitment activity data (for chart)
    /// </summary>
    public class WeeklyActivityDto
    {
        public List<string> Labels { get; set; } = new(); // ["Wk 1", "Wk 2", ...]
        public List<int> ApplicationsData { get; set; } = new(); // Số applications mỗi tuần
        public List<int> HiresData { get; set; } = new(); // Số hires mỗi tuần
    }
}

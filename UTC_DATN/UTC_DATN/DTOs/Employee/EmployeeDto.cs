namespace UTC_DATN.DTOs.Employee
{
    public class EmployeeDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Audit Trail
        public DateTime? LockedAt { get; set; }
        public string? LockedByName { get; set; }
        public string? LockReason { get; set; }
    }

    /// <summary>Request body khi vô hiệu hóa nhân viên (có thể thêm lý do)</summary>
    public class DeactivateRequest
    {
        public string? Reason { get; set; }
    }
}

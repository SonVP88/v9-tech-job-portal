namespace UTC_DATN.DTOs.Employee
{
    public class EmployeeDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Organization & Hierarchy
        public Guid? DepartmentId { get; set; }
        public string? DepartmentCode { get; set; }
        public string? DepartmentName { get; set; }
        public Guid? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public int SubordinatesCount { get; set; }

        // Audit Trail
        public DateTime? LockedAt { get; set; }
        public string? LockedByName { get; set; }
        public string? LockReason { get; set; }
    }

    public class DepartmentLookupDto
    {
        public Guid DepartmentId { get; set; }
        public string DepartmentCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int EmployeeCount { get; set; }
    }

    public class ManagerLookupDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
    }

    /// <summary>Request body khi vô hiệu hóa nhân viên (có thể thêm lý do)</summary>
    public class DeactivateRequest
    {
        public string? Reason { get; set; }
    }
}

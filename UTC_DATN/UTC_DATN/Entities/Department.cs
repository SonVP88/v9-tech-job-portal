// Entity mới: Department (Phòng ban / Khu vực chuyên môn)
// Phục vụ mô hình quản trị Matrix Organization trong hệ thống ATS Khu Công nghiệp
#nullable disable
using System;
using System.Collections.Generic;

namespace UTC_DATN.Entities;

public class Department
{
    /// <summary>
    /// Khóa chính - GUID auto-generate
    /// </summary>
    public Guid DepartmentId { get; set; }

    /// <summary>
    /// Mã phòng ban (Unique): VD: HR_TEAM, WH_LOGS, PROD_LINE
    /// </summary>
    public string DepartmentCode { get; set; }

    /// <summary>
    /// Tên đầy đủ của phòng ban / khu vực
    /// </summary>
    public string DepartmentName { get; set; }

    /// <summary>
    /// Mô tả vai trò / chức năng của bộ phận
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    /// <summary>
    /// Danh sách nhân viên/user thuộc phòng ban này
    /// </summary>
    public virtual ICollection<User> Users { get; set; } = new List<User>();

    /// <summary>
    /// Danh sách Job Posting tuyển dụng cho phòng ban này
    /// </summary>
    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();
}

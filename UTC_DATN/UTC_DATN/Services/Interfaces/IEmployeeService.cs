using UTC_DATN.DTOs.Employee;

namespace UTC_DATN.Services.Interfaces
{
    public interface IEmployeeService
    {
        /// <summary>
        /// Lấy danh sách nhân viên (HR và INTERVIEWER)
        /// </summary>
        Task<List<EmployeeDto>> GetEmployeesAsync();

        /// <summary>
        /// Tạo nhân viên mới
        /// </summary>
        Task<EmployeeDto?> CreateEmployeeAsync(CreateEmployeeRequest request);
        Task<bool> DeactivateEmployeeAsync(Guid userId, Guid adminId, string adminName, string? reason = null);
        Task<bool> ReactivateEmployeeAsync(Guid userId);
        Task<EmployeeDto?> UpdateEmployeeAsync(Guid userId, CreateEmployeeRequest request);
    }
}

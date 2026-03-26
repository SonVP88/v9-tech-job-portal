using UTC_DATN.DTOs.Application;

namespace UTC_DATN.Services.Interfaces;

public interface IApplicationService
{
    /// <summary>
    /// Xử lý nộp hồ sơ ứng tuyển
    /// </summary>
    /// <param name="request">Thông tin ứng tuyển bao gồm file CV</param>
    /// <param name="userId">ID của User đăng nhập (nullable - cho phép apply không cần login)</param>
    /// <returns>True nếu thành công, False nếu thất bại</returns>
    Task<bool> ApplyJobAsync(ApplyJobRequest request, Guid? userId);

    /// <summary>
    /// Lấy danh sách hồ sơ ứng tuyển theo JobId (Dành cho HR)
    /// </summary>
    Task<List<ApplicationDto>> GetApplicationsByJobIdAsync(Guid jobId);
    
    /// <summary>
    /// Cập nhật trạng thái hồ sơ ứng tuyển (Dành cho HR/Admin)
    /// </summary>
    Task<UpdateApplicationStatusResponse?> UpdateStatusAsync(Guid applicationId, string newStatus, bool isHrAction = true);


    /// <summary>
    /// Lấy danh sách hồ sơ ứng tuyển của ứng viên đã đăng nhập
    /// </summary>
    Task<List<MyApplicationDto>> GetMyApplicationsAsync(Guid userId);

    /// <summary>
    /// Ghi nhận lượt xem CV của HR/Admin
    /// </summary>
    Task<bool> TrackViewAsync(Guid applicationId, Guid viewerId);

    /// <summary>
    /// Lấy toàn bộ danh sách hồ sơ ứng tuyển (Dành cho HR/Admin)
    /// </summary>
    Task<List<ApplicationDto>> GetAllApplicationsAsync();
}

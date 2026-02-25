using UTC_DATN.Entities;

namespace UTC_DATN.Services.Interfaces
{
    /// <summary>
    /// Service để lấy Master Data cho các dropdown
    /// </summary>
    public interface IMasterDataService
    {
        /// <summary>
        /// Lấy toàn bộ danh sách Skills
        /// </summary>
        /// <returns>Danh sách Skills</returns>
        Task<List<Skill>> GetAllSkillsAsync();

        /// <summary>
        /// Lấy toàn bộ danh sách JobTypes
        /// </summary>
        /// <returns>Danh sách JobTypes</returns>
        Task<List<JobType>> GetAllJobTypesAsync();

        /// <summary>
        /// Lấy danh sách tất cả Tỉnh/Thành phố từ API
        /// </summary>
        /// <returns>Danh sách Tỉnh/Thành phố</returns>
        Task<List<Models.ProvinceDto>> GetProvincesAsync();

        /// <summary>
        /// Lấy danh sách Phường/Xã theo mã tỉnh (V2 API - bỏ cấp huyện)
        /// </summary>
        /// <param name="provinceCode">Mã tỉnh</param>
        /// <returns>Danh sách Phường/Xã</returns>
        Task<List<Models.WardDto>> GetWardsAsync(int provinceCode);
    }
}

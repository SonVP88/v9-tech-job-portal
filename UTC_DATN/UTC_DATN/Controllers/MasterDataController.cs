using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Controllers
{
    [Route("api/master-data")] // Đổi từ [controller] sang master-data
    [ApiController]
    [AllowAnonymous] // Cho phép truy cập không cần authentication
    public class MasterDataController : ControllerBase
    {
        private readonly IMasterDataService _masterDataService;

        public MasterDataController(IMasterDataService masterDataService)
        {
            _masterDataService = masterDataService;
        }

        /// <summary>
        /// Lấy danh sách tất cả Skills để hiển thị trên dropdown
        /// </summary>
        /// <returns>Danh sách Skills</returns>
        [HttpGet("skills")]
        public async Task<IActionResult> GetSkills()
        {
            try
            {
                var skills = await _masterDataService.GetAllSkillsAsync();
                return Ok(skills);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách Skills", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách tất cả JobTypes để hiển thị trên dropdown
        /// </summary>
        /// <returns>Danh sách JobTypes</returns>
        [HttpGet("job-types")]
        public async Task<IActionResult> GetJobTypes()
        {
            try
            {
                var jobTypes = await _masterDataService.GetAllJobTypesAsync();
                return Ok(jobTypes);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách JobTypes", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách tất cả Tỉnh/Thành phố từ API công khai
        /// </summary>
        /// <returns>Danh sách Tỉnh/Thành phố</returns>
        [HttpGet("provinces")]
        public async Task<IActionResult> GetProvinces()
        {
            try
            {
                var provinces = await _masterDataService.GetProvincesAsync();
                return Ok(provinces);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách tỉnh/thành phố", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách Phường/Xã theo mã tỉnh (V2 API - bỏ cấp huyện)
        /// </summary>
        /// <param name="provinceCode">Mã tỉnh</param>
        /// <returns>Danh sách Phường/Xã</returns>
        [HttpGet("provinces/{provinceCode}/wards")]
        public async Task<IActionResult> GetWardsByProvince(int provinceCode)
        {
            try
            {
                var wards = await _masterDataService.GetWardsAsync(provinceCode);
                return Ok(wards);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách phường/xã", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách các Phân xưởng / Phòng ban sản xuất trong hệ thống
        /// </summary>
        /// <returns>Danh sách phòng ban/phân xưởng</returns>
        [HttpGet("departments")]
        public async Task<IActionResult> GetDepartments()
        {
            try
            {
                var departments = await _masterDataService.GetDepartmentsAsync();
                return Ok(departments);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lấy danh sách phòng ban / phân xưởng", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách ngày lễ Việt Nam theo năm
        /// </summary>
        [HttpGet("vietnam-holidays/{year}")]
        public IActionResult GetVietnamHolidays(int year)
        {
            var holidays = new[]
            {
                "01-01", // Tết Dương Lịch
                "17-02", "18-02", "19-02", "20-02", "21-02", "22-02", "23-02", // Tết Nguyên Đán
                "30-04", // Ngày Giải phóng
                "01-05", // Quốc tế Lao động
                "02-09"  // Quốc khánh
            };
            return Ok(new { year, holidays });
        }
    }
}

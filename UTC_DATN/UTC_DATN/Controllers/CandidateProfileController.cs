using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UTC_DATN.DTOs.Candidate;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Controllers
{
    [ApiController]
    [Route("api/candidate")]
    [Authorize] // Yêu cầu đăng nhập
    public class CandidateProfileController : ControllerBase
    {
        private readonly ICandidateProfileService _profileService;

        public CandidateProfileController(ICandidateProfileService profileService)
        {
            _profileService = profileService;
        }

        /// <summary>
        /// GET /api/candidate/profile - Lấy thông tin profile của user hiện tại
        /// </summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                Console.WriteLine("DEBUG: Request received at /api/candidate/profile");
                
                // Lấy UserId từ token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    Console.WriteLine("DEBUG: No UserId claim found in token");
                    return Unauthorized(new { message = "Không thể xác thực người dùng" });
                }

                if (!Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    Console.WriteLine($"DEBUG: Invalid UserId format: {userIdClaim.Value}");
                    return Unauthorized(new { message = "User ID không hợp lệ" });
                }

                Console.WriteLine($"DEBUG: UserId from token: {userId}");

                var profile = await _profileService.GetProfileAsync(userId);

                if (profile == null)
                {
                    Console.WriteLine($"DEBUG: Profile service returned null for userId: {userId}");
                    return NotFound(new { message = "Không tìm thấy hồ sơ ứng viên" });
                }

                Console.WriteLine("DEBUG: Profile found, returning OK");
                return Ok(profile);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Exception in GetProfile: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/candidate/profile - Cập nhật thông tin profile
        /// </summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateCandidateProfileDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "Không thể xác thực người dùng" });
                }

                var result = await _profileService.UpdateProfileAsync(userId, dto);

                if (!result)
                {
                    return NotFound(new { message = "Không tìm thấy hồ sơ ứng viên" });
                }

                return Ok(new { message = "Cập nhật hồ sơ thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/candidate/upload-cv - Upload CV file
        /// </summary>
        [HttpPost("upload-cv")]
        public async Task<IActionResult> UploadCV(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "Vui lòng chọn file để tải lên" });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "Không thể xác thực người dùng" });
                }

                var fileUrl = await _profileService.UploadCVAsync(userId, file);

                return Ok(new { message = "Tải lên CV thành công", url = fileUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// DELETE /api/candidate/cv/{id} - Xóa CV file
        /// </summary>
        [HttpDelete("cv/{id}")]
        public async Task<IActionResult> DeleteCV(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "Không thể xác thực người dùng" });
                }

                var result = await _profileService.DeleteCVAsync(userId, id);

                if (!result)
                {
                    return NotFound(new { message = "Không tìm thấy CV hoặc không có quyền xóa" });
                }

                return Ok(new { message = "Xóa CV thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/candidate/upload-avatar - Upload Avatar
        /// </summary>
        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "Vui lòng chọn ảnh để tải lên" });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "Không thể xác thực người dùng" });
                }

                var avatarUrl = await _profileService.UploadAvatarAsync(userId, file);

                return Ok(new { message = "Cập nhật ảnh đại diện thành công", url = avatarUrl });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        /// <summary>
        /// PUT /api/candidate/cv/{id}/primary - Đặt CV làm mặc định
        /// </summary>
        [HttpPut("cv/{id}/primary")]
        public async Task<IActionResult> SetPrimaryCV(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "Không thể xác thực người dùng" });
                }

                var result = await _profileService.SetPrimaryDocumentAsync(userId, id);

                if (!result)
                {
                    return NotFound(new { message = "Không tìm thấy CV" });
                }

                return Ok(new { message = "Đã đặt CV làm mặc định" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/candidate/cv/{id}/rename - Đổi tên hiển thị CV
        /// </summary>
        [HttpPut("cv/{id}/rename")]
        public async Task<IActionResult> RenameCV(Guid id, [FromBody] RenameDocumentDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.NewName))
                {
                    return BadRequest(new { message = "Tên mới không được để trống" });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized(new { message = "Không thể xác thực người dùng" });
                }

                var result = await _profileService.UpdateDocumentNameAsync(userId, id, dto.NewName);

                if (!result)
                {
                    return NotFound(new { message = "Không tìm thấy CV" });
                }

                return Ok(new { message = "Đổi tên CV thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }
    }
}

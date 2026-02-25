using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UTC_DATN.Data;
using UTC_DATN.DTOs.Account;

namespace UTC_DATN.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly UTC_DATNContext _context;

        public AccountController(UTC_DATNContext context)
        {
            _context = context;
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            
            if (userIdClaim == null)
            {
                userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            }

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return userId;
            }
            return Guid.Empty;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return NotFound("User not found");

            // Assuming single role for simplicity or verify implementation
            var role = user.UserRoles.FirstOrDefault()?.Role?.Name ?? "Candidate";

            return Ok(new UserProfileDto
            {
                UserId = user.UserId,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                Role = role,
                AvatarUrl = user.AvatarUrl ?? ""
            });
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");

            user.FullName = dto.FullName;
            user.Phone = dto.Phone;
            if (!string.IsNullOrEmpty(dto.AvatarUrl))
            {
                user.AvatarUrl = dto.AvatarUrl;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Profile updated successfully" });
        }

        [HttpGet("company")]
        public async Task<IActionResult> GetCompanyInfo()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");

            return Ok(new CompanyInfoDto
            {
                Name = user.CompanyName,
                Website = user.CompanyWebsite,
                Industry = user.CompanyIndustry,
                Address = user.CompanyAddress,
                Description = user.CompanyDescription,
                LogoUrl = user.CompanyLogoUrl ?? ""
            });
        }

        [HttpGet("company-info")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicCompanyInfo()
        {
            var user = await _context.Users
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Admin"))
                .FirstOrDefaultAsync();

            if (user == null) return NotFound(new { message = "Company info not found" });

            return Ok(new CompanyInfoDto
            {
                Name = user.CompanyName,
                Website = user.CompanyWebsite,
                Industry = user.CompanyIndustry,
                Address = user.CompanyAddress,
                Description = user.CompanyDescription,
                LogoUrl = user.CompanyLogoUrl ?? ""
            });
        }

        [HttpPut("company")]
        public async Task<IActionResult> UpdateCompanyInfo([FromBody] UpdateCompanyDto dto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");

            user.CompanyName = dto.Name;
            user.CompanyWebsite = dto.Website;
            user.CompanyIndustry = dto.Industry;
            user.CompanyAddress = dto.Address;
            user.CompanyDescription = dto.Description;
            user.CompanyLogoUrl = dto.LogoUrl;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Company info updated successfully" });
        }
    }
}

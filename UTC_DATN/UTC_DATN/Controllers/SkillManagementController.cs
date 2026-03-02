using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UTC_DATN.Data;
using UTC_DATN.Entities;

namespace UTC_DATN.Controllers
{
    [ApiController]
    [Route("api/admin/skills")]
    [Authorize(Roles = "ADMIN")]
    public class SkillManagementController : ControllerBase
    {
        private readonly UTC_DATNContext _context;

        public SkillManagementController(UTC_DATNContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách tất cả Skills (có phân trang và tìm kiếm)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSkills([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _context.Skills.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(s => s.Name.ToLower().Contains(searchLower));
            }

            var total = await query.CountAsync();
            var skills = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                data = skills,
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }

        /// <summary>
        /// Thêm Skill mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateSkill([FromBody] CreateSkillRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Tên kỹ năng không được để trống" });
            }

            var normalizedName = request.Name.ToUpper();

            // Kiểm tra trùng
            var exists = await _context.Skills.AnyAsync(s => s.NormalizedName == normalizedName);
            if (exists)
            {
                return BadRequest(new { message = "Kỹ năng này đã tồn tại" });
            }

            var skill = new Skill
            {
                SkillId = Guid.NewGuid(),
                Name = request.Name.Trim(),
                NormalizedName = normalizedName,
                CreatedAt = DateTime.UtcNow
            };

            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSkills), new { id = skill.SkillId }, skill);
        }

        /// <summary>
        /// Cập nhật Skill
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSkill(Guid id, [FromBody] CreateSkillRequest request)
        {
            var skill = await _context.Skills.FindAsync(id);
            if (skill == null)
            {
                return NotFound(new { message = "Không tìm thấy kỹ năng" });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Tên kỹ năng không được để trống" });
            }

            var normalizedName = request.Name.ToUpper();

            // Kiểm tra trùng (trừ chính nó)
            var exists = await _context.Skills.AnyAsync(s => s.NormalizedName == normalizedName && s.SkillId != id);
            if (exists)
            {
                return BadRequest(new { message = "Kỹ năng này đã tồn tại" });
            }

            skill.Name = request.Name.Trim();
            skill.NormalizedName = normalizedName;

            await _context.SaveChangesAsync();

            return Ok(skill);
        }

        /// <summary>
        /// Khóa / Mở khóa Skill
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> ToggleDeleteSkill(Guid id)
        {
            var skill = await _context.Skills
                .Include(s => s.JobSkillMaps)
                .Include(s => s.CandidateSkills)
                .FirstOrDefaultAsync(s => s.SkillId == id);

            if (skill == null)
            {
                return NotFound(new { message = "Không tìm thấy kỹ năng" });
            }

            // Chuyển đổi trạng thái Ẩn/Hiện bằng cột IsDeleted
            skill.IsDeleted = !skill.IsDeleted;
            await _context.SaveChangesAsync();

            var statusStr = skill.IsDeleted ? "Đã khóa" : "Đã mở khóa";
            return Ok(new { message = $"{statusStr} kỹ năng thành công" });
        }
    }

    public class CreateSkillRequest
    {
        public string Name { get; set; } = string.Empty;
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using UTC_DATN.Data;
using UTC_DATN.DTOs.Candidate;
using UTC_DATN.Entities;
using UTC_DATN.Services.Interfaces;

namespace UTC_DATN.Services.Implements
{
    public class CandidateProfileService : ICandidateProfileService
    {
        private readonly UTC_DATNContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public CandidateProfileService(
            UTC_DATNContext context,
            IWebHostEnvironment env,
            IConfiguration config)
        {
            _context = context;
            _env = env;
            _config = config;
        }

        public async Task<CandidateProfileDto?> GetProfileAsync(Guid userId)
        {
            Console.WriteLine($"DEBUG SERVICE: Getting profile for {userId}");
            
            // Lấy thông tin user để check
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);
            if (user == null)
            {
                Console.WriteLine($"DEBUG SERVICE: User not found in Users table for ID {userId}");
                return null;
            }

            var candidate = await _context.Candidates
                .AsNoTracking() // Ensure fresh data
                .Include(c => c.CandidateSkills)
                    .ThenInclude(cs => cs.Skill)
                .Include(c => c.CandidateDocuments)
                    .ThenInclude(cd => cd.File)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (candidate == null)
            {
                Console.WriteLine("DEBUG SERVICE: Candidate profile not found, creating new one...");
                candidate = new Candidate
                {
                    CandidateId = Guid.NewGuid(),
                    UserId = userId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = null,
                    Location = null,
                    Headline = null,
                    Summary = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false,
                    LinkedIn = null,
                    GitHub = null,
                    Avatar = null
                };

                _context.Candidates.Add(candidate);
                await _context.SaveChangesAsync();
                Console.WriteLine("DEBUG SERVICE: New candidate profile created and saved");

                // Reload để có relationships
                candidate = await _context.Candidates
                    .Include(c => c.CandidateSkills)
                        .ThenInclude(cs => cs.Skill)
                    .Include(c => c.CandidateDocuments)
                        .ThenInclude(cd => cd.File)
                    .FirstOrDefaultAsync(c => c.CandidateId == candidate.CandidateId);
            }

            // DEBUG LOGGING
            if (candidate != null && candidate.CandidateDocuments != null)
            {
                foreach (var d in candidate.CandidateDocuments)
                {
                    Console.WriteLine($"DEBUG SERVICE: DocID={d.CandidateDocumentId}, DisplayName='{d.DisplayName}', FileName='{d.File?.OriginalFileName}'");
                }
            }

            return new CandidateProfileDto
            {
                CandidateId = candidate!.CandidateId,
                FullName = candidate.FullName,
                Email = candidate.Email,
                Phone = candidate.Phone,
                Location = candidate.Location,
                Headline = candidate.Headline,
                Summary = candidate.Summary,
                LinkedIn = candidate.LinkedIn,
                GitHub = candidate.GitHub,
                Avatar = candidate.Avatar,
                Skills = candidate.CandidateSkills.Select(cs => new CandidateSkillDto
                {
                    SkillId = cs.SkillId,
                    SkillName = cs.Skill?.Name ?? "",
                    Level = cs.Level,
                    Years = cs.Years
                }).ToList(),
                Documents = candidate.CandidateDocuments.Select(cd => new CandidateDocumentDto
                {
                    DocumentId = cd.CandidateDocumentId,
                    FileName = cd.File?.OriginalFileName ?? "",
                    FileUrl = cd.File?.Url ?? "",
                    DocType = cd.DocType,
                    SizeBytes = cd.File?.SizeBytes,
                    CreatedAt = cd.CreatedAt,
                    IsPrimary = cd.IsPrimary,
                    DisplayName = (!string.IsNullOrWhiteSpace(cd.DisplayName) ? cd.DisplayName + " [DB]" : (cd.File?.OriginalFileName + " [FILE]" ?? ""))
                }).ToList()
            };
        }

        /// <summary>
        /// Lấy profile ứng viên theo CandidateId (dùng cho HR/Admin xem chi tiết ứng viên)
        /// </summary>
        public async Task<CandidateProfileDto?> GetProfileByCandidateIdAsync(Guid candidateId)
        {
            var candidate = await _context.Candidates
                .AsNoTracking()
                .Include(c => c.CandidateSkills)
                    .ThenInclude(cs => cs.Skill)
                .Include(c => c.CandidateDocuments)
                    .ThenInclude(cd => cd.File)
                .FirstOrDefaultAsync(c => c.CandidateId == candidateId && !c.IsDeleted);

            if (candidate == null) return null;

            return new CandidateProfileDto
            {
                CandidateId = candidate.CandidateId,
                FullName = candidate.FullName,
                Email = candidate.Email,
                Phone = candidate.Phone,
                Location = candidate.Location,
                Headline = candidate.Headline,
                Summary = candidate.Summary,
                LinkedIn = candidate.LinkedIn,
                GitHub = candidate.GitHub,
                Avatar = candidate.Avatar,
                Skills = candidate.CandidateSkills.Select(cs => new CandidateSkillDto
                {
                    SkillId = cs.SkillId,
                    SkillName = cs.Skill?.Name ?? "",
                    Level = cs.Level,
                    Years = cs.Years
                }).ToList(),
                Documents = candidate.CandidateDocuments.Select(cd => new CandidateDocumentDto
                {
                    DocumentId = cd.CandidateDocumentId,
                    FileName = cd.File?.OriginalFileName ?? "",
                    FileUrl = cd.File?.Url ?? "",
                    DocType = cd.DocType,
                    SizeBytes = cd.File?.SizeBytes,
                    CreatedAt = cd.CreatedAt,
                    IsPrimary = cd.IsPrimary,
                    DisplayName = !string.IsNullOrWhiteSpace(cd.DisplayName) ? cd.DisplayName : (cd.File?.OriginalFileName ?? "")
                }).ToList()
            };
        }


        public async Task<bool> UpdateProfileAsync(Guid userId, UpdateCandidateProfileDto dto)
        {
            var candidate = await _context.Candidates
                .Include(c => c.CandidateSkills)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (candidate == null)
                return false;

            // Cập nhật thông tin cơ bản
            candidate.FullName = dto.FullName;
            candidate.Phone = dto.Phone;
            candidate.Location = dto.Location;
            candidate.Headline = dto.Headline;
            candidate.Summary = dto.Summary;
            candidate.LinkedIn = dto.LinkedIn;
            candidate.GitHub = dto.GitHub;
            // Avatar is usually handled via upload, but if sent as string (e.g. url), map it
            if (!string.IsNullOrEmpty(dto.Avatar)) 
            {
                candidate.Avatar = dto.Avatar;
            }
            candidate.UpdatedAt = DateTime.UtcNow;

            // Cập nhật Skills: Xóa hết skills cũ
            if (candidate.CandidateSkills != null && candidate.CandidateSkills.Any())
            {
                _context.CandidateSkills.RemoveRange(candidate.CandidateSkills);
            }

            // Xử lý dto.Skills (danh sách tên kỹ năng từ frontend)
            var finalSkillIds = new List<Guid>();

            // Ưu tiên 1: Nếu frontend gửi SkillIds hợp lệ (chọn từ autocomplete)
            var validSkillIds = dto.SkillIds.Where(id => id != Guid.Empty).Distinct().ToList();
            if (validSkillIds.Any())
            {
                // Xác minh rằng các SkillId này thực sự tồn tại trong DB
                var existingIds = await _context.Skills
                    .Where(s => validSkillIds.Contains(s.SkillId))
                    .Select(s => s.SkillId)
                    .ToListAsync();
                finalSkillIds.AddRange(existingIds);
            }

            // Ưu tiên 2: Fallback - xử lý skills không có ID (do tự nhập)
            if (dto.Skills != null && dto.Skills.Any())
            {
                // Xác định chỉ số skills tương ứng với Guid.Empty
                var skillNamesToProcess = new List<string>();
                for (int i = 0; i < dto.Skills.Count; i++)
                {
                    var hasId = i < dto.SkillIds.Count && dto.SkillIds[i] != Guid.Empty;
                    if (!hasId)
                    {
                        skillNamesToProcess.Add(dto.Skills[i]);
                    }
                }

                foreach (var skillName in skillNamesToProcess)
                {
                    if (string.IsNullOrWhiteSpace(skillName)) continue;

                    var normalizedName = skillName.Trim().ToUpper();

                    // 1. Tìm hoặc tạo mới trong bảng Skills
                    var skill = await _context.Skills
                        .FirstOrDefaultAsync(s => s.NormalizedName == normalizedName);

                    if (skill == null)
                    {
                        skill = new Skill
                        {
                            SkillId = Guid.NewGuid(),
                            Name = skillName.Trim(),
                            NormalizedName = normalizedName,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Skills.Add(skill);
                        await _context.SaveChangesAsync();
                    }

                    if (!finalSkillIds.Contains(skill.SkillId))
                    {
                        finalSkillIds.Add(skill.SkillId);
                    }
                }
            }

            // Gắn link vào bảng trung gian CandidateSkills
            foreach (var skillId in finalSkillIds.Distinct())
            {
                    candidate.CandidateSkills.Add(new CandidateSkill
                    {
                        CandidateId = candidate.CandidateId,
                        SkillId = skillId,
                        Level = (byte)1, // Beginner mặc định
                        Years = 0m
                    });
            }

            await _context.SaveChangesAsync();
            return true;
        }



        public async Task<string> UploadCVAsync(Guid userId, IFormFile file)
        {
            var candidate = await _context.Candidates
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (candidate == null)
                throw new Exception("Không tìm thấy hồ sơ ứng viên");

            // Validate file
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
                throw new Exception("Chỉ chấp nhận file PDF, DOC, DOCX");

            if (file.Length > 10 * 1024 * 1024) // 10MB
                throw new Exception("Kích thước file không được vượt quá 10MB");

            // Tạo thư mục lưu trữ
            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "cvs", userId.ToString());
            Directory.CreateDirectory(uploadFolder);

            // Tạo tên file unique
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadFolder, fileName);

            // Lưu file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Tính SHA256
            string sha256;
            using (var sha = SHA256.Create())
            {
                using var fileStream = System.IO.File.OpenRead(filePath);
                var hash = await sha.ComputeHashAsync(fileStream);
                sha256 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }

            // Tạo URL
            var baseUrl = _config["AppSettings:BaseUrl"] ?? "https://localhost:7181";
            var fileUrl = $"{baseUrl}/uploads/cvs/{userId}/{fileName}";

            // Tạo File entity
            var fileEntity = new Entities.File
            {
                FileId = Guid.NewGuid(),
                Provider = "LOCAL",
                OriginalFileName = file.FileName,
                StoredFileName = fileName,
                MimeType = file.ContentType,
                SizeBytes = file.Length,
                Sha256 = sha256,
                Url = fileUrl,
                LocalPath = filePath,
                CreatedAt = DateTime.UtcNow
            };

            _context.Files.Add(fileEntity);

            _context.Files.Add(fileEntity);

            // Kiểm tra xem đã có CV nào chưa? Nếu chưa thì set là Primary
            var hasCv = await _context.CandidateDocuments.AnyAsync(cd => cd.CandidateId == candidate.CandidateId);
            
            // Tạo CandidateDocument
            var candidateDocument = new CandidateDocument
            {
                CandidateDocumentId = Guid.NewGuid(),
                CandidateId = candidate.CandidateId,
                FileId = fileEntity.FileId,
                DocType = "CV",
                CreatedAt = DateTime.UtcNow,
                IsPrimary = !hasCv, // Nếu là CV đầu tiên thì set là Primary
                DisplayName = file.FileName // Mặc định DisplayName là tên file gốc
            };

            _context.CandidateDocuments.Add(candidateDocument);
            await _context.SaveChangesAsync();

            return fileUrl;
        }






        public async Task<bool> DeleteCVAsync(Guid userId, Guid documentId)
        {
            try 
            {
                var candidate = await _context.Candidates
                    .Include(c => c.CandidateDocuments)
                        .ThenInclude(cd => cd.File)
                    .Include(c => c.CandidateDocuments)
                        .ThenInclude(cd => cd.Applications)
                    .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

                if (candidate == null) return false;

                var document = candidate.CandidateDocuments.FirstOrDefault(d => d.CandidateDocumentId == documentId || d.FileId == documentId);
                
                if (document == null) return false;

                // Check dependencies
                if (document.Applications != null && document.Applications.Any())
                {
                    // Allow soft delete logic or just block?
                    // For now, block with message. Valid strategy for "My Profile".
                    throw new Exception($"CV này đang được sử dụng trong {document.Applications.Count} đơn ứng tuyển. Không thể xóa.");
                }

                // Xóa Document link TRƯỚC (để tránh lỗi FK nếu có cascade restrict)
                _context.CandidateDocuments.Remove(document);

                // Delete physical file if exists
                if (document.File != null)
                {
                    if (!string.IsNullOrEmpty(document.File.LocalPath) && System.IO.File.Exists(document.File.LocalPath))
                    {
                        try
                        {
                            System.IO.File.Delete(document.File.LocalPath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error deleting file: {ex.Message}");
                        }
                    }
                    
                    // Sau đó mới xóa File entity
                    _context.Files.Remove(document.File);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteCVAsync: {ex.Message}");
                if (ex.InnerException != null)
                {
                     Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                throw; // Re-throw to controller
            }
        }

        public async Task<string> UploadAvatarAsync(Guid userId, IFormFile file)
        {
            var candidate = await _context.Candidates
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (candidate == null)
                throw new Exception("Không tìm thấy hồ sơ ứng viên");

             // Validate image
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
                throw new Exception("Chỉ chấp nhận file ảnh (JPG, PNG, GIF, WEBP)");

            if (file.Length > 5 * 1024 * 1024) // 5MB
                throw new Exception("Kích thước ảnh không được vượt quá 5MB");

            // Tạo thư mục lưu trữ avatar
            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "avatars", userId.ToString());
            Directory.CreateDirectory(uploadFolder);

            // Tạo tên file unique
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadFolder, fileName);

            // Lưu file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Tạo URL
            var baseUrl = _config["AppSettings:BaseUrl"] ?? "https://localhost:7181"; // Fallback to https for images
            var fileUrl = $"{baseUrl}/uploads/avatars/{userId}/{fileName}";

            // Update database
            candidate.Avatar = fileUrl;
            candidate.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return fileUrl;
        }

        public async Task<bool> SetPrimaryDocumentAsync(Guid userId, Guid documentId)
        {
            var candidate = await _context.Candidates
                .Include(c => c.CandidateDocuments)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (candidate == null) return false;

            var targetDoc = candidate.CandidateDocuments.FirstOrDefault(d => d.CandidateDocumentId == documentId);
            if (targetDoc == null) return false;

            // Set all to false
            foreach (var doc in candidate.CandidateDocuments)
            {
                doc.IsPrimary = false;
            }

            // Set target to true
            targetDoc.IsPrimary = true;
            targetDoc.DisplayName = targetDoc.DisplayName; // Trigger update status if needed

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateDocumentNameAsync(Guid userId, Guid documentId, string newName)
        {
             var candidate = await _context.Candidates
                .Include(c => c.CandidateDocuments)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (candidate == null) return false;

            var targetDoc = candidate.CandidateDocuments.FirstOrDefault(d => d.CandidateDocumentId == documentId);
            if (targetDoc == null) return false;

            targetDoc.DisplayName = newName;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

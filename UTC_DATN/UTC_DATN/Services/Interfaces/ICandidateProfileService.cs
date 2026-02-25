using Microsoft.AspNetCore.Http;
using UTC_DATN.DTOs.Candidate;

namespace UTC_DATN.Services.Interfaces
{
    public interface ICandidateProfileService
    {
        /// <summary>
        /// Lấy thông tin profile của candidate theo UserId
        /// </summary>
        Task<CandidateProfileDto?> GetProfileAsync(Guid userId);

        /// <summary>
        /// Cập nhật thông tin profile và skills
        /// </summary>
        Task<bool> UpdateProfileAsync(Guid userId, UpdateCandidateProfileDto dto);

        /// <summary>
        /// Upload CV file và trả về URL
        /// </summary>
        Task<string> UploadCVAsync(Guid userId, IFormFile file);

        /// <summary>
        /// Xóa CV theo DocumentId
        /// </summary>
        Task<bool> DeleteCVAsync(Guid userId, Guid documentId);

        /// <summary>
        /// Upload Avatar và trả về URL
        /// </summary>
        Task<string> UploadAvatarAsync(Guid userId, IFormFile file);

        Task<bool> SetPrimaryDocumentAsync(Guid userId, Guid documentId);
        Task<bool> UpdateDocumentNameAsync(Guid userId, Guid documentId, string newName);
    }
}

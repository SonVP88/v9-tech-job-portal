using System;
using System.Threading.Tasks;
using UTC_DATN.DTOs;

namespace UTC_DATN.Services.Interfaces
{
    public interface INotificationSettingsService
    {
        Task<NotificationSettingDto> GetSettingsAsync(Guid userId);
        Task<NotificationSettingDto> UpdateSettingsAsync(Guid userId, NotificationSettingDto settings);
    }
}

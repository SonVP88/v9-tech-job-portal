using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UTC_DATN.DTOs;

namespace UTC_DATN.Services.Interfaces
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationDto>> GetNotificationsAsync(Guid userId);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task MarkAsReadAsync(Guid notificationId);
        Task MarkAllAsReadAsync(Guid userId);
        Task CreateNotificationAsync(Guid userId, string title, string message, string type, string relatedId = null);
        Task DeleteNotificationAsync(Guid notificationId);
    }
}

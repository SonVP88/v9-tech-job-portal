import { CommonModule } from '@angular/common';
import { Component, Inject, PLATFORM_ID, OnInit, OnDestroy, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { NotificationService, NotificationDto } from '../../services/notification.service';
import { ToastComponent } from '../../components/toast/toast.component';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-hr-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, ToastComponent],
  templateUrl: './hr-layout.html',
  styleUrl: './hr-layout.scss',
})
export class HrLayout implements OnInit, OnDestroy {
  private authService = inject(AuthService);
  unreadCount = 0;
  notifications: NotificationDto[] = [];
  isNotificationDropdownOpen = false;
  private pollingInterval: any;

  userFullName = 'Người dùng';
  userRole = 'Nhân viên';

  constructor(
    private notificationService: NotificationService,
    private router: Router,
    @Inject(PLATFORM_ID) private platformId: Object
  ) { }

  ngOnInit(): void {
    this.loadNotifications();
    this.loadUserFromToken();

    if (isPlatformBrowser(this.platformId)) {
      this.pollingInterval = setInterval(() => {
        this.loadNotifications();
      }, 15000);
    }
  }

  loadUserFromToken(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    try {
      const token = localStorage.getItem('authToken');
      if (!token) return;
      const payload = JSON.parse(atob(token.split('.')[1]));
      this.userFullName = payload.name
        || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name']
        || 'Người dùng';
      const rawRole = payload.role
        || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
        || '';
      const roleMap: Record<string, string> = {
        'HR': 'Nhân sự',
        'ADMIN': 'Quản trị viên',
        'CANDIDATE': 'Ứng viên',
      };
      this.userRole = roleMap[rawRole] || rawRole || 'Nhân viên';
    } catch { }
  }

  ngOnDestroy(): void {
    if (this.pollingInterval) {
      clearInterval(this.pollingInterval);
    }
  }

  loadNotifications(): void {
    this.notificationService.getUnreadCount().subscribe({
      next: (count) => this.unreadCount = count,
      error: (err) => console.error('Error fetching unread count:', err)
    });

    this.notificationService.getNotifications().subscribe({
      next: (data) => this.notifications = data,
      error: (err) => console.error('Error fetching notifications:', err)
    });
  }

  toggleNotificationDropdown(): void {
    this.isNotificationDropdownOpen = !this.isNotificationDropdownOpen;
  }

  markAsRead(n: NotificationDto): void {
    if (!n.isRead) {
      this.notificationService.markAsRead(n.id).subscribe({
        next: () => {
          n.isRead = true;
          this.unreadCount = Math.max(0, this.unreadCount - 1);
        }
      });
    }
  }

  markAllAsRead(): void {
    this.notificationService.markAllAsRead().subscribe({
      next: () => {
        this.notifications.forEach(n => n.isRead = true);
        this.unreadCount = 0;
      }
    });
  }

  formatDate(dateStr: string): string {
    // Thêm 'Z' để đảm bảo parse đúng UTC, tránh lệch 7 tiếng
    const utcStr = dateStr.endsWith('Z') ? dateStr : dateStr + 'Z';
    const date = new Date(utcStr);
    return date.toLocaleDateString('vi-VN', {
      hour: '2-digit',
      minute: '2-digit',
      day: '2-digit',
      month: '2-digit'
    });
  }

  previewCandidateView(): void {
    // Set flag to indicate admin preview mode
    localStorage.setItem('isAdminPreview', 'true');
    // Navigate to candidate home
    this.router.navigate(['/candidate/home']);
  }

  logout(): void {
    this.authService.logout();
  }
}

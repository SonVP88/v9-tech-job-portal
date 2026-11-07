import { CommonModule } from '@angular/common';
import { Component, Inject, PLATFORM_ID, OnInit, OnDestroy, inject, computed, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { NotificationService, NotificationDto } from '../../services/notification.service';
import { ToastComponent } from '../../components/toast/toast.component';
import { ToastService } from '../../services/toast.service';
import { AuthService } from '../../services/auth.service';
import { SIDEBAR_MENU_GROUPS, DEV_ACCOUNTS, MenuGroup } from '../../configs/menu.config';
import { CurrentUser } from '../../models/auth.model';

@Component({
  selector: 'app-hr-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, ToastComponent, FormsModule],
  templateUrl: './hr-layout.html',
  styleUrl: './hr-layout.scss',
})
export class HrLayout implements OnInit, OnDestroy {
  public authService = inject(AuthService);
  private notificationService = inject(NotificationService);
  private toastService = inject(ToastService);
  private router = inject(Router);
  @Inject(PLATFORM_ID) private platformId: Object = inject(PLATFORM_ID);

  unreadCount = 0;
  notifications: NotificationDto[] = [];
  isNotificationDropdownOpen = false;
  private pollingInterval: any;

  // Dynamic Badges Signals
  openJobsCount = signal<number>(14);
  pendingApplicationsCount = signal<number>(8);
  todayInterviewsCount = signal<number>(3);

  // Dev Role Switcher
  devAccounts = DEV_ACCOUNTS;
  isDevDropdownOpen = false;
  isDevMode = false;

  // Modal Đề xuất tuyển dụng (Dành cho Quản đốc phân xưởng)
  isHiringModalOpen = signal<boolean>(false);
  isSubmitting = signal<boolean>(false);
  requestForm = {
    jobTitle: '',
    targetHeadcount: 10,
    reason: 'EXPANSION',
    urgencyLevel: 'URGENT',
    shiftType: '3_SHIFTS',
    specialRequirements: ''
  };

  /**
   * Computed: Lọc 4 nhóm menu dựa trên quyền hạn của người dùng đăng nhập
   * Nếu tất cả các mục trong một nhóm đều bị ẩn -> Ẩn luôn tiêu đề của nhóm đó
   */
  visibleMenuGroups = computed<MenuGroup[]>(() => {
    const user = this.authService.currentUser();
    return SIDEBAR_MENU_GROUPS
      .map(group => ({
        ...group,
        items: group.items.filter(item => item.canAccess(user))
      }))
      .filter(group => group.items.length > 0);
  });

  /**
   * Computed: Avatar chữ cái đầu (Initials)
   */
  userInitials = computed<string>(() => {
    const name = this.authService.currentUser()?.fullName?.trim() || 'Admin';
    const words = name.split(/\s+/);
    if (words.length >= 2) {
      return (words[0][0] + words[words.length - 1][0]).toUpperCase();
    }
    return name.slice(0, 2).toUpperCase();
  });

  ngOnInit(): void {
    setTimeout(() => {
      this.loadNotifications();
    });

    if (isPlatformBrowser(this.platformId)) {
      this.pollingInterval = setInterval(() => {
        this.loadNotifications();
      }, 15000);
    }
  }

  ngOnDestroy(): void {
    if (this.pollingInterval) {
      clearInterval(this.pollingInterval);
    }
  }

  getBadgeCount(key?: 'openJobs' | 'pendingApplications' | 'todayInterviews'): number {
    switch (key) {
      case 'openJobs':
        return this.openJobsCount();
      case 'pendingApplications':
        return this.pendingApplicationsCount();
      case 'todayInterviews':
        return this.todayInterviewsCount();
      default:
        return 0;
    }
  }

  // Modal Đề xuất tuyển dụng Handlers
  openHiringRequestModal(): void {
    this.requestForm = {
      jobTitle: '',
      targetHeadcount: 10,
      reason: 'EXPANSION',
      urgencyLevel: 'URGENT',
      shiftType: '3_SHIFTS',
      specialRequirements: ''
    };
    this.isHiringModalOpen.set(true);
  }

  closeHiringRequestModal(): void {
    this.isHiringModalOpen.set(false);
  }

  submitHiringRequest(): void {
    if (!this.requestForm.jobTitle.trim()) {
      this.toastService.warning('Thiếu thông tin', 'Vui lòng nhập vị trí công việc cần tuyển dụng.');
      return;
    }

    if (this.requestForm.targetHeadcount <= 0) {
      this.toastService.warning('Số lượng không hợp lệ', 'Số lượng nhân sự cần tuyển phải lớn hơn 0.');
      return;
    }

    this.isSubmitting.set(true);

    setTimeout(() => {
      this.isSubmitting.set(false);
      this.isHiringModalOpen.set(false);
      const dept = this.authService.currentUser()?.departmentName || 'Xưởng chuyên môn';
      this.toastService.success(
        'Đề xuất thành công',
        `Đã gửi yêu cầu tuyển dụng "${this.requestForm.jobTitle}" (${this.requestForm.targetHeadcount} người) từ ${dept} tới Phòng Nhân Sự. Trạng thái: Chờ HR phê duyệt (PendingHRApproval).`
      );
    }, 600);
  }

  // Dev Role Switcher Handlers
  toggleDevDropdown(): void {
    this.isDevDropdownOpen = !this.isDevDropdownOpen;
  }

  onSelectDevAccount(account: CurrentUser): void {
    this.authService.switchDevUser(account);
    this.isDevDropdownOpen = false;
  }

  onRestoreRealAccount(): void {
    this.authService.restoreRealUser();
    this.isDevDropdownOpen = false;
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
    const utcStr = dateStr.endsWith('Z') ? dateStr : dateStr + 'Z';
    const date = new Date(utcStr);
    return date.toLocaleDateString('vi-VN', {
      hour: '2-digit',
      minute: '2-digit',
      day: '2-digit',
      month: '2-digit'
    });
  }

  logout(): void {
    this.authService.logout();
  }
}

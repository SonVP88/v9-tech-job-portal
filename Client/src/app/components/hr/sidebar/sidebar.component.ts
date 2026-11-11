import { Component, inject, computed, signal, OnInit, OnDestroy, effect, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../../services/auth.service';
import { SIDEBAR_MENU_GROUPS, DEV_ACCOUNTS, MenuGroup } from '../../../configs/menu.config';
import { CurrentUser } from '../../../models/auth.model';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html'
})
export class SidebarComponent implements OnInit, OnDestroy {
  public authService = inject(AuthService);
  private http = inject(HttpClient);
  private platformId = inject(PLATFORM_ID);

  // Dynamic Badges Signals (Đếm thời gian thực từ Database qua API)
  openJobsCount = signal<number>(0);
  pendingApplicationsCount = signal<number>(0);
  todayInterviewsCount = signal<number>(0);

  // Dev Role Switcher
  devAccounts = DEV_ACCOUNTS;
  isDevDropdownOpen = false;
  isDevMode = false;

  private refreshTimer: any;

  constructor() {
    // Tự động tải lại badges mỗi khi CurrentUser thay đổi (Đăng nhập / Đổi Role)
    effect(() => {
      const user = this.authService.currentUser();
      if (isPlatformBrowser(this.platformId) && user) {
        this.loadBadgeCounts();
      }
    });
  }

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.loadBadgeCounts();
      // Định kỳ 60s làm tươi số liệu badge một lần
      this.refreshTimer = setInterval(() => {
        this.loadBadgeCounts();
      }, 60000);
    }
  }

  ngOnDestroy(): void {
    if (this.refreshTimer) {
      clearInterval(this.refreshTimer);
    }
  }

  /**
   * Gọi API /api/dashboard/sidebar-badges để lấy số lượng thật từ Database
   */
  loadBadgeCounts(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    this.http.get<{ success: boolean; data: { openJobs: number; pendingApplications: number; todayInterviews: number } }>('/api/dashboard/sidebar-badges')
      .subscribe({
        next: (response) => {
          if (response && response.success && response.data) {
            this.openJobsCount.set(response.data.openJobs || 0);
            this.pendingApplicationsCount.set(response.data.pendingApplications || 0);
            this.todayInterviewsCount.set(response.data.todayInterviews || 0);
          }
        },
        error: (err) => {
          console.warn('[SidebarComponent] Không thể tải dữ liệu số đếm badges:', err);
        }
      });
  }

  /**
   * Computed: Lọc các nhóm Menu và các mục con theo quyền hạn người dùng.
   * Rule: Nếu tất cả các mục trong một nhóm đều bị ẩn đối với Role hiện tại,
   * ẩn luôn tiêu đề của nhóm đó.
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
   * Computed: Tạo Avatar Initials (chữ cái đầu) từ Họ và Tên người dùng
   */
  userInitials = computed<string>(() => {
    const name = this.authService.currentUser()?.fullName?.trim() || 'Admin';
    const words = name.split(/\s+/);
    if (words.length >= 2) {
      return (words[0][0] + words[words.length - 1][0]).toUpperCase();
    }
    return name.slice(0, 2).toUpperCase();
  });

  /**
   * Trả về số đếm badge động theo key
   */
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

  logout(): void {
    this.authService.logout();
  }
}

import { Component, inject, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { SIDEBAR_MENU_GROUPS, DEV_ACCOUNTS, MenuGroup } from '../../../configs/menu.config';
import { CurrentUser } from '../../../models/auth.model';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html'
})
export class SidebarComponent {
  public authService = inject(AuthService);

  // Dynamic Badges Signals (Mô phỏng đếm từ Database / API)
  openJobsCount = signal<number>(14);
  pendingApplicationsCount = signal<number>(8);
  todayInterviewsCount = signal<number>(3);

  // Dev Role Switcher
  devAccounts = DEV_ACCOUNTS;
  isDevDropdownOpen = false;
  isDevMode = true; // Bật cho môi trường dev & demo đồ án

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

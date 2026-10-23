import { Injectable, PLATFORM_ID, inject, NgZone, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { jwtDecode } from 'jwt-decode';
import { isPlatformBrowser } from '@angular/common';
import { environment } from '../../environments/environment';
import { ChatbotService } from './chatbot.service';
import { PopupService } from './popup.service';
import { CurrentUser, AppRole, DepartmentCode } from '../models/auth.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private platformId = inject(PLATFORM_ID);
  private chatbotService = inject(ChatbotService);
  private ngZone = inject(NgZone);
  private popup = inject(PopupService);
  private router = inject(Router);
  private http = inject(HttpClient);

  private idleTimer: any;
  private readonly IDLE_TIMEOUT = 5 * 60 * 1000; // 5 phút

  /**
   * Signal quản lý trạng thái người dùng hiện tại
   */
  currentUser = signal<CurrentUser | null>(null);

  /**
   * Computed Signal: Kiểm tra quyền SuperAdmin hoặc thuộc Ban Giám Đốc (ADMIN_SYS)
   */
  isSuperAdmin = computed(() => {
    const user = this.currentUser();
    if (!user) return false;
    return user.role === 'SuperAdmin' || user.role === 'ADMIN' || user.departmentCode === 'ADMIN_SYS';
  });

  /**
   * Computed Signal: Kiểm tra thuộc phòng Nhân sự (HR_TEAM / HR)
   */
  isHrMember = computed(() => {
    const user = this.currentUser();
    if (!user) return false;
    return user.departmentCode === 'HR_TEAM' || user.role === 'HR';
  });

  /**
   * Computed Signal: Kiểm tra cấp quản lý (SuperAdmin hoặc DepartmentManager của các phòng ban)
   */
  isManagerLevel = computed(() => {
    const user = this.currentUser();
    if (!user) return false;
    return (
      user.role === 'SuperAdmin' ||
      user.role === 'ADMIN' ||
      user.role === 'DepartmentManager' ||
      user.role === 'MANAGER' ||
      user.departmentCode === 'ADMIN_SYS'
    );
  });

  /**
   * Computed Signal: Chỉ trả về true nếu là SuperAdmin hoặc thuộc HR_TEAM
   */
  canPostJob = computed(() => {
    const user = this.currentUser();
    if (!user) return false;
    const isSuper = user.role === 'SuperAdmin' || user.role === 'ADMIN' || user.departmentCode === 'ADMIN_SYS';
    const isHr = user.departmentCode === 'HR_TEAM' || user.role === 'HR';
    return isSuper || isHr;
  });

  /**
   * Computed Signal: Kiểm tra là Quản đốc / Trưởng bộ phận của các xưởng kỹ thuật/sản xuất
   */
  isPlantManager = computed(() => {
    const user = this.currentUser();
    if (!user) return false;
    return (
      (user.role === 'DepartmentManager' || user.role === 'MANAGER') &&
      ['PROD_LINE', 'WH_LOGS', 'TECH_MNT', 'QA_QC'].includes(user.departmentCode || '')
    );
  });

  constructor() {
    this.initCurrentUser();
    this.startIdleTimer();
  }

  /**
   * Khởi tạo CurrentUser từ sessionStorage (dev mode) hoặc giải mã JWT Token trong localStorage
   */
  initCurrentUser(): CurrentUser | null {
    if (!isPlatformBrowser(this.platformId) || typeof window === 'undefined') {
      return null;
    }

    try {
      // 1. Kiểm tra tài khoản mô phỏng khi test (Dev Role Switcher)
      const simulatedUserJson = sessionStorage.getItem('dev_simulated_user');
      if (simulatedUserJson) {
        const simulatedUser: CurrentUser = JSON.parse(simulatedUserJson);
        this.currentUser.set(simulatedUser);
        return simulatedUser;
      }

      // 2. Đọc token từ localStorage
      const token = localStorage.getItem('authToken');
      if (!token) {
        this.currentUser.set(null);
        return null;
      }

      const user = this.decodeJwtToken(token);
      this.currentUser.set(user);
      return user;
    } catch (error) {
      console.error('Lỗi khi khởi tạo CurrentUser từ Token:', error);
      this.currentUser.set(null);
      return null;
    }
  }

  /**
   * Giải mã JWT Token thành CurrentUser object chuẩn ATS
   */
  decodeJwtToken(token: string): CurrentUser | null {
    try {
      const decoded: any = jwtDecode(token);
      const rawRole = (decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || decoded['role'] || 'Staff') as AppRole;
      const rawFullName = decoded['FullName'] || decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || decoded['name'] || 'Người dùng';
      const rawEmail = decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || decoded['email'] || '';
      const rawId = decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || decoded['sub'] || decoded['id'] || '';
      const deptCode = (decoded['DepartmentCode'] || decoded['departmentCode']) as DepartmentCode | undefined;
      const deptId = decoded['DepartmentId'] || decoded['departmentId'];
      const deptName = decoded['DepartmentName'] || decoded['departmentName'];

      return {
        id: rawId,
        fullName: rawFullName,
        email: rawEmail,
        role: rawRole,
        roleDisplayName: this.getRoleDisplayName(rawRole),
        departmentId: deptId ? String(deptId) : undefined,
        departmentCode: deptCode,
        departmentName: deptName
      };
    } catch (error) {
      console.error('Lỗi giải mã JWT Token:', error);
      return null;
    }
  }

  /**
   * Helper chuyển Role code sang Role Display Name tiếng Việt thân thiện
   */
  getRoleDisplayName(role: string): string {
    switch (role) {
      case 'SuperAdmin':
      case 'ADMIN':
        return 'Ban Giám Đốc';
      case 'DepartmentManager':
      case 'MANAGER':
        return 'Trưởng Phòng / Quản Đốc';
      case 'TeamLeader':
        return 'Tổ Trưởng';
      case 'Staff':
        return 'Nhân Viên';
      case 'HR':
        return 'Chuyên Viên Tuyển Dụng';
      case 'INTERVIEWER':
        return 'Phỏng Vấn Viên Chuyên Môn';
      default:
        return role || 'Người dùng';
    }
  }

  /**
   * Dev Role Switcher: Chuyển nhanh vai trò để test phân quyền mà không cần đăng nhập lại
   */
  switchDevUser(user: CurrentUser): void {
    if (!isPlatformBrowser(this.platformId) || typeof window === 'undefined') return;
    console.log(`[Dev Role Switcher] Switched to: ${user.fullName} (${user.role} - ${user.departmentCode})`);
    sessionStorage.setItem('dev_simulated_user', JSON.stringify(user));
    this.currentUser.set(user);
  }

  /**
   * Khôi phục lại tài khoản thật đăng nhập từ JWT Token
   */
  restoreRealUser(): void {
    if (!isPlatformBrowser(this.platformId) || typeof window === 'undefined') return;
    sessionStorage.removeItem('dev_simulated_user');
    this.initCurrentUser();
    console.log('[Dev Role Switcher] Restored real logged-in user.');
  }

  startIdleTimer(): void {
    if (!isPlatformBrowser(this.platformId) || typeof window === 'undefined') return;

    this.ngZone.runOutsideAngular(() => {
      ['mousemove', 'keydown', 'click', 'scroll', 'touchstart'].forEach(eventName => {
        window.addEventListener(eventName, () => this.resetIdleTimer());
      });
    });

    this.resetIdleTimer();
  }

  resetIdleTimer(): void {
    if (!isPlatformBrowser(this.platformId) || typeof window === 'undefined') return;

    if (this.idleTimer) {
      clearTimeout(this.idleTimer);
    }

    if (!this.isAuthenticated()) return;

    this.ngZone.runOutsideAngular(() => {
      this.idleTimer = setTimeout(() => {
        this.ngZone.run(() => {
          console.log('Phiên đăng nhập hết hạn do không hoạt động trong 5 phút.');
          this.logout(true);
        });
      }, this.IDLE_TIMEOUT);
    });
  }

  stopIdleTimer(): void {
    if (this.idleTimer) {
      clearTimeout(this.idleTimer);
      this.idleTimer = null;
    }
  }

  isAuthenticated(): boolean {
    if (!isPlatformBrowser(this.platformId)) return false;
    if (typeof window === 'undefined') return false;

    // Nếu đang có dev simulated user
    if (sessionStorage.getItem('dev_simulated_user')) return true;

    const token = localStorage.getItem('authToken');
    return !!token;
  }

  /**
   * Tương thích ngược với các components cũ đang gọi getCurrentUser()
   */
  getCurrentUser(): any {
    const user = this.currentUser();
    if (user) {
      return {
        ...user,
        userId: user.id,
        name: user.fullName
      };
    }
    return this.initCurrentUser();
  }

  getUserRole(): string | null {
    const user = this.currentUser();
    return user ? user.role : null;
  }

  logout(isTimeout: boolean = false): void {
    if (isPlatformBrowser(this.platformId) && typeof window !== 'undefined') {
      console.log('🚪 Đăng xuất - Xóa token khỏi localStorage');
      localStorage.removeItem('authToken');
      sessionStorage.removeItem('dev_simulated_user');
      this.currentUser.set(null);
    }

    this.stopIdleTimer();
    this.chatbotService.clearChat();

    this.router.navigate(['/login']).then(() => {
      if (isTimeout) {
        void this.popup.alert({
          title: 'Phiên hết hạn',
          message: 'Phiên đăng nhập đã tự động kết thúc do không hoạt động trong 5 phút. Vui lòng đăng nhập lại.',
          confirmText: 'Đăng nhập',
          tone: 'neutral',
        });
      }
    });
  }

  saveToken(token: string): boolean {
    if (!isPlatformBrowser(this.platformId) || typeof window === 'undefined') {
      console.warn(' SSR: Cannot save token on server side');
      return false;
    }

    try {
      localStorage.setItem('authToken', token);
      sessionStorage.removeItem('dev_simulated_user');
      this.initCurrentUser();
      this.resetIdleTimer();
      console.log(' Token saved and CurrentUser initialized successfully');
      return true;
    } catch (error) {
      console.error(' Error saving token:', error);
      return false;
    }
  }

  changePassword(data: any): Observable<any> {
    return this.http.post(`${environment.apiUrl}/auth/change-password`, data);
  }

  forgotPassword(email: string): Observable<any> {
    return this.http.post(`${environment.apiUrl}/auth/forgot-password`, { email });
  }

  googleLogin(idToken: string): Observable<any> {
    return this.http.post(`${environment.apiUrl}/auth/google-login`, { idToken });
  }

  linkGoogle(idToken: string): Observable<any> {
    return this.http.post(`${environment.apiUrl}/auth/link-google`, { idToken });
  }
}

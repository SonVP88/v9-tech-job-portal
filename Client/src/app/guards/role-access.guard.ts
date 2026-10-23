import { inject, PLATFORM_ID } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from '@angular/router';
import { isPlatformBrowser } from '@angular/common';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';
import { SIDEBAR_MENU_ITEMS } from '../configs/menu.config';
import { CurrentUser } from '../models/auth.model';

/**
 * Functional Route Guard (CanActivateFn) bảo vệ các route Admin/HR
 * theo quyền hạn Role và DepartmentCode dựa trên SIDEBAR_MENU_ITEMS
 */
export const roleAccessGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot
): boolean | UrlTree => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const toastService = inject(ToastService);
  const platformId = inject(PLATFORM_ID);

  // Cho phép pass qua khi SSR render trên Server
  if (!isPlatformBrowser(platformId)) {
    return true;
  }

  // Đảm bảo thông tin user đã được load
  let user = authService.currentUser();
  if (!user && authService.isAuthenticated()) {
    user = authService.initCurrentUser();
  }

  // Nếu chưa đăng nhập -> Chuyển về trang login
  if (!authService.isAuthenticated() || !user) {
    toastService.warning('Yêu cầu đăng nhập', 'Vui lòng đăng nhập để tiếp tục.');
    return router.createUrlTree(['/login']);
  }

  // 1. Kiểm tra nếu route có cấu hình hàm canAccess riêng trong route.data['canAccess']
  const routeCanAccess = route.data?.['canAccess'] as ((u: CurrentUser | null) => boolean) | undefined;
  if (typeof routeCanAccess === 'function') {
    if (routeCanAccess(user)) {
      return true;
    }
    toastService.error('Truy cập bị từ chối', 'Bạn không có quyền truy cập chức năng này!');
    return router.createUrlTree(['/hr/dashboard']);
  }

  // 2. Đối chiếu URL hiện tại với danh sách SIDEBAR_MENU_ITEMS
  const rawUrl = state.url.split('?')[0].split('#')[0].replace(/\/+$/, '');

  const matchedMenuItem = SIDEBAR_MENU_ITEMS.find(item => {
    const itemRoute = item.route.replace(/\/+$/, '');
    // Khớp chính xác route hoặc là route con trực tiếp (vd: /hr/post-job/123)
    return rawUrl === itemRoute || rawUrl.startsWith(itemRoute + '/');
  });

  // Nếu không nằm trong danh mục bảo vệ của Menu -> Cho phép truy cập
  if (!matchedMenuItem) {
    return true;
  }

  // 3. Kiểm tra quyền truy cập theo cấu hình Menu
  const hasAccess = matchedMenuItem.canAccess(user);

  if (hasAccess) {
    return true;
  }

  // Người dùng không đủ quyền truy cập -> Báo lỗi Toast và chuyển hướng về /hr/dashboard
  console.warn(`[roleAccessGuard] Access denied for user "${user.fullName}" (${user.role} - ${user.departmentCode}) on path: ${state.url}`);
  toastService.error('Truy cập bị từ chối', 'Bạn không có quyền truy cập chức năng này!');
  return router.createUrlTree(['/hr/dashboard']);
};

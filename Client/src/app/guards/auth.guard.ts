import { inject, PLATFORM_ID } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';
import { isPlatformBrowser } from '@angular/common';

/**
 * Guard cho các trang yêu cầu đăng nhập (không phân biệt role)
 * Nếu chưa đăng nhập → hiện toast cảnh báo, sau đó redirect về /login kèm returnUrl
 */
export const authGuard: CanActivateFn = (route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);
    const toast = inject(ToastService);
    const platformId = inject(PLATFORM_ID);

    // SSR: Cho phép trên server, kiểm tra trên client
    if (!isPlatformBrowser(platformId)) {
        return true;
    }

    const isAuth = authService.isAuthenticated();

    if (!isAuth) {
        // Hiện toast cảnh báo trước
        toast.warning(
            'Yêu cầu đăng nhập',
            'Vui lòng đăng nhập để tiếp tục!'
        );
        // Redirect về /login kèm returnUrl để sau login quay lại đúng trang
        router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
        return false;
    }

    return true;
};

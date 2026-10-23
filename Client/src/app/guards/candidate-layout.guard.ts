import { inject, PLATFORM_ID } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { isPlatformBrowser } from '@angular/common';

/**
 * Guard dùng cho các trang Candidate (như /home, /jobs).
 * Nếu đang login bằng Role ADMIN/HR, tự động đá về Dashboard để không kẹt Giao diện Candidate.
 */
export const candidateLayoutGuard: CanActivateFn = (route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);
    const platformId = inject(PLATFORM_ID);

    if (!isPlatformBrowser(platformId)) {
        return true;
    }

    if (authService.isAuthenticated()) {
        const role = authService.getUserRole();
        // Nếu là Nhân sự/Quản lý/Admin -> tự động chuyển sang Dashboard
        if (role === 'ADMIN' || role === 'HR' || role === 'MANAGER' || role === 'INTERVIEWER') {
            const isAdminPreview = localStorage.getItem('isAdminPreview') === 'true';
            if (!isAdminPreview) {
                router.navigate(['/hr/dashboard']);
                return false;
            }
        }
    }

    return true;
};

import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

/**
 * Danh sách các đường dẫn công khai mà khách chưa đăng nhập được phép truy cập tự do,
 * không được tự động đá về /login khi có lỗi 401 từ các request nền.
 */
function isPublicPage(url: string): boolean {
    const cleanUrl = (url || '').split('?')[0];
    return cleanUrl.startsWith('/candidate/home') ||
           cleanUrl.startsWith('/candidate/jobs') ||
           cleanUrl.startsWith('/candidate/job-detail') ||
           cleanUrl.startsWith('/candidate/company') ||
           cleanUrl.startsWith('/apply') ||
           cleanUrl.startsWith('/login') ||
           cleanUrl.startsWith('/register') ||
           cleanUrl.startsWith('/403');
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const router = inject(Router);

    // Tránh truy cập `localStorage` khi chạy trên server (SSR) -> ReferenceError
    let token: string | null = null;
    if (typeof window !== 'undefined' && typeof window.localStorage !== 'undefined') {
        token = window.localStorage.getItem('authToken') || window.localStorage.getItem('auth_token');
    }

    let requestToSend = req;

    // Nếu có token, thêm vào header Authorization
    if (token) {
        requestToSend = req.clone({
            setHeaders: {
                Authorization: `Bearer ${token}`
            }
        });
    }

    return next(requestToSend).pipe(
        catchError((error: HttpErrorResponse) => {
            // Khi token hết hạn hoặc không hợp lệ (401 Unauthorized)
            if (error.status === 401) {
                console.warn('⚠️ [AuthInterceptor] 401 Unauthorized.');
                if (typeof window !== 'undefined' && typeof window.localStorage !== 'undefined') {
                    window.localStorage.removeItem('authToken');
                    window.localStorage.removeItem('auth_token');
                    window.sessionStorage.removeItem('dev_simulated_user');
                }

                const currentUrl = router.url;

                // Chỉ tự động điều hướng sang /login nếu:
                // 1. Trước đó người dùng ĐÃ có token (phiên làm việc thực sự bị hết hạn)
                // 2. VÀ trang hiện tại KHÔNG PHẢI là trang công khai (như xem chi tiết job, xem trang chủ, form apply...)
                if (token && !isPublicPage(currentUrl)) {
                    router.navigate(['/login'], { queryParams: { sessionExpired: 'true', returnUrl: currentUrl } });
                }
            }
            return throwError(() => error);
        })
    );
};

import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

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
                console.warn('⚠️ [AuthInterceptor] 401 Unauthorized: Phiên đăng nhập đã hết hạn.');
                if (typeof window !== 'undefined' && typeof window.localStorage !== 'undefined') {
                    window.localStorage.removeItem('authToken');
                    window.localStorage.removeItem('auth_token');
                    window.sessionStorage.removeItem('dev_simulated_user');
                }

                // Không redirect nếu đang ở trang login hoặc register
                const currentUrl = router.url;
                if (!currentUrl.includes('/login') && !currentUrl.includes('/register') && !currentUrl.includes('/apply/')) {
                    router.navigate(['/login'], { queryParams: { sessionExpired: 'true' } });
                }
            }
            return throwError(() => error);
        })
    );
};

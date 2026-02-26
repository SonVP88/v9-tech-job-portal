import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { jwtDecode } from 'jwt-decode';
import { isPlatformBrowser } from '@angular/common';
import { environment } from '../../environments/environment';

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private platformId = inject(PLATFORM_ID);

    constructor(private router: Router, private http: HttpClient) { }

    isAuthenticated(): boolean {
        // ⚡ SSR Fix: Only access localStorage in browser
        if (!isPlatformBrowser(this.platformId)) return false;
        if (typeof window === 'undefined') return false;

        const token = localStorage.getItem('authToken');
        return !!token;
    }


    getCurrentUser(): any {
        // ⚡ SSR Fix: Only access localStorage in browser
        if (!isPlatformBrowser(this.platformId)) return null;
        if (typeof window === 'undefined') return null;

        const token = localStorage.getItem('authToken');
        if (!token) return null;

        try {
            const decoded: any = jwtDecode(token);
            return {
                userId: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || decoded['sub'],
                email: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || decoded['email'],
                name: decoded['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || decoded['name'],
                role: decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || decoded['role']
            };
        } catch (error) {
            console.error('Lỗi decode token:', error);
            return null;
        }
    }

    getUserRole(): string | null {
        const user = this.getCurrentUser();
        return user ? user.role : null;
    }

    logout(): void {
        // ⚡ SSR Fix: Only access localStorage in browser
        if (isPlatformBrowser(this.platformId) && typeof window !== 'undefined') {
            console.log('🚪 Đăng xuất - Xóa token khỏi localStorage');
            localStorage.removeItem('authToken');

            const remainingToken = localStorage.getItem('authToken');
            if (remainingToken) {
                console.error(' Cảnh báo: Token vẫn còn trong localStorage!');
            }
        }

        this.router.navigate(['/login']);
    }

    saveToken(token: string): boolean {
        // ⚡ SSR Fix: Only access localStorage in browser
        if (!isPlatformBrowser(this.platformId)) {
            console.warn(' SSR: Cannot save token on server side');
            return false;
        }
        if (typeof window === 'undefined') return false;

        try {
            localStorage.setItem('authToken', token);
            console.log(' Token saved successfully');
            return true;
        } catch (error) {
            console.error(' Error saving token:', error);
            return false;
        }
    }

    // New method for Change Password
    changePassword(data: any): Observable<any> {
        return this.http.post(`${environment.apiUrl}/auth/change-password`, data);
    }
}

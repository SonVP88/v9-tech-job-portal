import { Injectable, PLATFORM_ID, inject, NgZone } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { jwtDecode } from 'jwt-decode';
import { isPlatformBrowser } from '@angular/common';
import { environment } from '../../environments/environment';
import { ChatbotService } from './chatbot.service';

@Injectable({
    providedIn: 'root'
})
export class AuthService {
    private platformId = inject(PLATFORM_ID);
    private chatbotService = inject(ChatbotService);
    private ngZone = inject(NgZone);

    private idleTimer: any;
    private readonly IDLE_TIMEOUT = 5 * 60 * 1000; // 5 phút

    constructor(private router: Router, private http: HttpClient) { 
        this.startIdleTimer();
    }

    startIdleTimer(): void {
        if (!isPlatformBrowser(this.platformId) || typeof window === 'undefined') return;

        // Chạy ngoài NgZone để tránh trigger Change Detection khi user di chuột hoặc gõ phím liên tục
        this.ngZone.runOutsideAngular(() => {
            ['mousemove', 'keydown', 'click', 'scroll', 'touchstart'].forEach(eventName => {
                window.addEventListener(eventName, () => this.resetIdleTimer());
            });
        });

        this.resetIdleTimer();
    }

    resetIdleTimer(): void {
        if (!isPlatformBrowser(this.platformId) || typeof window === 'undefined') return;
        
        // Đoạn này có thể gọi liên tục nên cần check nhanh
        if (this.idleTimer) {
            clearTimeout(this.idleTimer);
        }

        // Chỉ xử lý đăng xuất khi đã xác thực
        if (!this.isAuthenticated()) return;

        this.ngZone.runOutsideAngular(() => {
            this.idleTimer = setTimeout(() => {
                this.ngZone.run(() => {
                    console.log('Phiên đăng nhập hết hạn do không hoạt động trong 5 phút.');
                    this.logout(true); // Gửi cờ báo hiệu đăng xuất do timeout
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

        const token = localStorage.getItem('authToken');
        return !!token;
    }


    getCurrentUser(): any {
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

    logout(isTimeout: boolean = false): void {
        if (isPlatformBrowser(this.platformId) && typeof window !== 'undefined') {
            console.log('🚪 Đăng xuất - Xóa token khỏi localStorage');
            localStorage.removeItem('authToken');

            const remainingToken = localStorage.getItem('authToken');
            if (remainingToken) {
                console.error(' Cảnh báo: Token vẫn còn trong localStorage!');
            }
        }

        this.stopIdleTimer();

        // Xóa hoàn toàn lịch sử trò chuyện Chatbot khỏi RAM
        this.chatbotService.clearChat();

        this.router.navigate(['/login']).then(() => {
            if (isTimeout) {
                alert('Phiên đăng nhập đã tự động kết thúc do không hoạt động trong 5 phút. Vui lòng đăng nhập lại.');
            }
        });
    }

    saveToken(token: string): boolean {
        if (!isPlatformBrowser(this.platformId)) {
            console.warn(' SSR: Cannot save token on server side');
            return false;
        }
        if (typeof window === 'undefined') return false;

        try {
            localStorage.setItem('authToken', token);
            console.log(' Token saved successfully');
            
            // Bắt đầu đếm thời gian timeout ngay khi vừa lưu token xong
            this.resetIdleTimer();
            
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

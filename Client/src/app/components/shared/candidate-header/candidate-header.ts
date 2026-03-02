import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { NotificationService, NotificationDto } from '../../../services/notification.service';

@Component({
    selector: 'app-candidate-header',
    standalone: true,
    imports: [CommonModule, RouterModule],
    templateUrl: './candidate-header.html',
})
export class CandidateHeaderComponent implements OnInit {
    isLoggedIn = false;
    userFullName = '';
    userRole = '';

    unreadCount = 0;
    notifications: NotificationDto[] = [];
    isNotificationDropdownOpen = false;

    // Admin Preview Mode
    isAdminPreview = false;

    constructor(
        private authService: AuthService,
        private notificationService: NotificationService,
        private router: Router
    ) { }

    private pollingInterval: any;

    ngOnInit(): void {
        if (typeof window !== 'undefined' && window.localStorage) {
            // Check Admin Preview Mode
            this.isAdminPreview = localStorage.getItem('isAdminPreview') === 'true';

            // Check xem có token không - sử dụng key 'authToken' đúng với hệ thống
            const token = localStorage.getItem('authToken');
            this.isLoggedIn = !!token;

            if (this.isLoggedIn && token) {
                try {
                    // Parse JWT token để lấy thông tin user
                    const payload = JSON.parse(atob(token.split('.')[1]));

                    // Lấy thông tin từ JWT claims (hỗ trợ cả short name và full claim name)
                    this.userRole = payload.role ||
                        payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ||
                        'CANDIDATE';

                    this.userFullName = payload.name ||
                        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ||
                        'User';

                    console.log('Header loaded user:', this.userFullName, 'Role:', this.userRole);

                    // Load notifications if candidate
                    if (this.userRole === 'CANDIDATE') {
                        this.loadNotifications();

                        // Setup polling every 30 seconds
                        this.pollingInterval = setInterval(() => {
                            this.loadNotifications();
                        }, 30000);
                    }
                } catch (e) {
                    console.error('Error parsing token in header:', e);
                    this.userFullName = 'User';
                    this.userRole = 'CANDIDATE';
                }
            } else {
                console.log('No token found in header component');
            }
        }
    }

    ngOnDestroy(): void {
        if (this.pollingInterval) {
            clearInterval(this.pollingInterval);
        }
    }

    logout(): void {
        this.authService.logout();
    }

    loadNotifications(): void {
        this.notificationService.getUnreadCount().subscribe({
            next: (count) => this.unreadCount = count,
            error: (err) => console.error('Error fetching unread count:', err)
        });

        this.notificationService.getNotifications().subscribe({
            next: (data) => this.notifications = data,
            error: (err) => console.error('Error fetching notifications:', err)
        });
    }

    toggleNotificationDropdown(): void {
        this.isNotificationDropdownOpen = !this.isNotificationDropdownOpen;
        if (this.isNotificationDropdownOpen && this.unreadCount > 0) {
            // Optional: Auto mark as read when opening? 
            // Better to do it manually or on specific click
        }
    }

    markAsRead(n: NotificationDto): void {
        if (!n.isRead) {
            this.notificationService.markAsRead(n.id).subscribe({
                next: () => {
                    n.isRead = true;
                    this.unreadCount = Math.max(0, this.unreadCount - 1);
                }
            });
        }
    }

    markAllAsRead(): void {
        this.notificationService.markAllAsRead().subscribe({
            next: () => {
                this.notifications.forEach(n => n.isRead = true);
                this.unreadCount = 0;
            }
        });
    }

    formatDate(dateStr: string): string {
        const date = new Date(dateStr);
        return date.toLocaleDateString('vi-VN', {
            hour: '2-digit',
            minute: '2-digit',
            day: '2-digit',
            month: '2-digit'
        });
    }

    backToAdmin(): void {
        // Remove preview flag
        localStorage.removeItem('isAdminPreview');
        // Navigate back to admin dashboard
        this.router.navigate(['/hr/dashboard']);
    }

    toggleUserMenu(): void {
        // Close user menu after navigation click
        // (menu auto-closes on route change, this handles edge cases)
    }
}

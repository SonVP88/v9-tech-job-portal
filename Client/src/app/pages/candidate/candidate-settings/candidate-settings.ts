import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { BehaviorSubject } from 'rxjs';
import { CandidateHeaderComponent } from '../../../components/shared/candidate-header/candidate-header';
import { CandidateSettingsService, NotificationSettings } from '../../../services/candidate-settings.service';

type Tab = 'security' | 'notifications';

@Component({
    selector: 'app-candidate-settings',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule, CandidateHeaderComponent],
    templateUrl: './candidate-settings.html',
    styleUrl: './candidate-settings.scss'
})
export class CandidateSettingsComponent implements OnInit {
    activeTab: Tab = 'security';

    // ─── Password ─────────────────────────────────────────────────────────────
    currentPassword = '';
    newPassword = '';
    confirmPassword = '';
    showCurrent = false;
    showNew = false;
    showConfirm = false;

    private pwdMsgSubject = new BehaviorSubject<{ text: string; ok: boolean } | null>(null);
    pwdMsg$ = this.pwdMsgSubject.asObservable();
    pwdLoading = false;

    // ─── Notifications ────────────────────────────────────────────────────────
    private notifSubject = new BehaviorSubject<NotificationSettings | null>(null);
    notif$ = this.notifSubject.asObservable();

    private notifMsgSubject = new BehaviorSubject<{ text: string; ok: boolean } | null>(null);
    notifMsg$ = this.notifMsgSubject.asObservable();
    notifLoading = false;

    constructor(private settingsService: CandidateSettingsService) { }

    ngOnInit(): void {
        this.loadNotificationSettings();
    }

    setTab(tab: Tab): void {
        this.activeTab = tab;
    }

    // ─── Password ─────────────────────────────────────────────────────────────
    changePassword(): void {
        this.pwdMsgSubject.next(null);
        if (!this.currentPassword || !this.newPassword || !this.confirmPassword) {
            this.pwdMsgSubject.next({ text: 'Vui lòng điền đầy đủ thông tin', ok: false });
            return;
        }
        if (this.newPassword !== this.confirmPassword) {
            this.pwdMsgSubject.next({ text: 'Mật khẩu xác nhận không khớp', ok: false });
            return;
        }
        if (this.newPassword.length < 6) {
            this.pwdMsgSubject.next({ text: 'Mật khẩu phải có ít nhất 6 ký tự', ok: false });
            return;
        }
        this.pwdLoading = true;
        this.settingsService.changePassword(this.currentPassword, this.newPassword, this.confirmPassword)
            .subscribe({
                next: (res) => {
                    this.pwdLoading = false;
                    this.pwdMsgSubject.next({ text: res.message, ok: true });
                    this.currentPassword = '';
                    this.newPassword = '';
                    this.confirmPassword = '';
                },
                error: (err) => {
                    this.pwdLoading = false;
                    const msg = err.error?.message || 'Đổi mật khẩu thất bại';
                    this.pwdMsgSubject.next({ text: msg, ok: false });
                }
            });
    }

    // ─── Notifications ────────────────────────────────────────────────────────
    loadNotificationSettings(): void {
        this.settingsService.getNotificationSettings().subscribe({
            next: (data) => this.notifSubject.next(data),
            error: () => this.notifSubject.next({
                notifyJobOpportunities: true,
                notifyApplicationUpdates: true,
                notifySecurityAlerts: true,
                notifyMarketing: false,
                channelEmail: true,
                channelPush: true
            })
        });
    }

    saveNotifications(): void {
        const current = this.notifSubject.getValue();
        if (!current) return;
        this.notifLoading = true;
        this.notifMsgSubject.next(null);
        this.settingsService.updateNotificationSettings(current).subscribe({
            next: (res) => {
                this.notifLoading = false;
                this.notifMsgSubject.next({ text: res.message, ok: true });
            },
            error: () => {
                this.notifLoading = false;
                this.notifMsgSubject.next({ text: 'Cập nhật thất bại', ok: false });
            }
        });
    }

    toggle(key: keyof NotificationSettings): void {
        const current = this.notifSubject.getValue();
        if (!current) return;
        this.notifSubject.next({ ...current, [key]: !current[key] });
    }
}

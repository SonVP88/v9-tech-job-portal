import { Component, NgZone, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Title } from '@angular/platform-browser';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AccountService, UserProfileDto, CompanyInfoDto, NotificationSettingDto } from '../../../services/account.service';
import { AuthService } from '../../../services/auth.service';
import { ToastService } from '../../../services/toast.service';
import {
    ApplicationService,
    SlaStageConfigDto,
    UpdateSlaStageConfigRequest
} from '../../../services/application.service';
import { forkJoin } from 'rxjs';

@Component({
    selector: 'app-settings',
    standalone: true,
    imports: [CommonModule, FormsModule, ReactiveFormsModule],
    templateUrl: './settings.html',
})
export class SettingsComponent implements OnInit {
    activeTab: 'account' | 'company' | 'notifications' | 'sla' = 'account';
    isLoading = false;
    isUploadingAvatar = false;
    isUploadingLogo = false;
    avatarPreview: string | null = null;
    logoPreview: string | null = null;
    userProfileEmail: string = '';
    userRole: string = '';

    accountForm!: FormGroup;
    companyForm!: FormGroup;
    passwordFormGroup!: FormGroup;

    // UI Model for Notifications
    notificationUi = {
        emailNewApplicant: true,
        emailInterviewReminder: true,
        systemAlerts: true
    };

    // Backend DTO
    private notificationDto: NotificationSettingDto = {
        notifyJobOpportunities: true,
        notifyApplicationUpdates: true,
        notifySecurityAlerts: true,
        notifyMarketing: false,
        channelEmail: true,
        channelPush: true
    };

    slaStageConfigs: SlaStageConfigDto[] = [];
    isLoadingSla = false;

    constructor(
        private titleService: Title,
        private accountService: AccountService,
        private authService: AuthService,
        private http: HttpClient,
        private ngZone: NgZone,
        private fb: FormBuilder,
        private cdr: ChangeDetectorRef,
        private toast: ToastService,
        private applicationService: ApplicationService
    ) { }

    ngOnInit(): void {
        this.titleService.setTitle('Cài Đặt - Quản Trị Hệ Thống');
        this.initForms();
        this.loadData();
        this.loadSlaConfigs();
    }

    initForms(): void {
        this.accountForm = this.fb.group({
            fullName: ['', Validators.required],
            phone: ['']
        });

        this.companyForm = this.fb.group({
            name: ['', Validators.required],
            website: [''],
            industry: [''],
            address: [''],
            description: ['']
        });

        this.passwordFormGroup = this.fb.group({
            oldPassword: ['', Validators.required],
            newPassword: ['', [Validators.required, Validators.minLength(6)]],
            confirmPassword: ['', Validators.required]
        });
    }

    loadData(): void {
        this.accountService.getProfile().subscribe({
            next: (data) => this.ngZone.run(() => {
                this.accountForm.patchValue({
                    fullName: data.fullName,
                    phone: data.phone
                });
                this.userProfileEmail = data.email;
                this.userRole = data.role;
                this.avatarPreview = data.avatarUrl || null;
                setTimeout(() => {
                    this.cdr.detectChanges();
                });
            }),
            error: (err) => console.error('Failed to load profile', err)
        });

        // 2. Load Company Info
        this.accountService.getCompanyInfo().subscribe({
            next: (data) => this.ngZone.run(() => {
                this.companyForm.patchValue({
                    name: data.name,
                    website: data.website,
                    industry: data.industry,
                    address: data.address,
                    description: data.description
                });
                this.logoPreview = data.logoUrl || null;
                setTimeout(() => {
                    this.cdr.detectChanges();
                });
            }),
            error: (err) => console.error('Failed to load company info', err)
        });

        // 3. Load Notifications
        this.accountService.getNotificationSettings().subscribe({
            next: (data) => this.ngZone.run(() => {
                this.notificationDto = data;
                this.notificationUi.emailNewApplicant = data.notifyApplicationUpdates;
                this.notificationUi.emailInterviewReminder = data.notifyJobOpportunities;
                this.notificationUi.systemAlerts = data.channelPush;
                setTimeout(() => {
                    this.cdr.detectChanges();
                });
            }),
            error: (err) => console.error('Failed to load notifications', err)
        });
    }

    /**
     * Triggered when user clicks the avatar edit button
     * Opens hidden file input
     */
    triggerAvatarUpload(): void {
        const fileInput = document.getElementById('avatarInput') as HTMLInputElement;
        fileInput?.click();
    }

    /**
     * Handles avatar file selection and upload
     */
    onAvatarSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        if (!input.files || input.files.length === 0) return;

        const file = input.files[0];

        const validTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
        if (!validTypes.includes(file.type)) {
            this.toast.warning('Sai định dạng', 'Chỉ chấp nhận file ảnh (jpg, png, gif, webp)!');
            return;
        }

        // Validate size (5MB)
        if (file.size > 5 * 1024 * 1024) {
            this.toast.warning('Kích thước quá lớn', 'File không được vượt quá 5MB!');
            return;
        }


        const reader = new FileReader();
        reader.onload = (e) => this.ngZone.run(() => {
            this.avatarPreview = e.target?.result as string;
            this.cdr.detectChanges();
        });
        reader.readAsDataURL(file);

        this.isUploadingAvatar = true;
        const token = localStorage.getItem('authToken');
        const headers = new HttpHeaders({ 'Authorization': `Bearer ${token}` });

        const formData = new FormData();
        formData.append('file', file);

        this.http.post<{ url: string }>('/api/fileupload/avatar', formData, { headers }).subscribe({
            next: (res) => this.ngZone.run(() => {
                this.avatarPreview = res.url;
                this.isUploadingAvatar = false;
                this.cdr.detectChanges();
                this.updateProfile();
            }),
            error: (err) => this.ngZone.run(() => {
                console.error('Avatar upload failed', err);
                this.toast.error('Tải lên thất bại', err.error?.message || 'Lỗi không xác định');
                this.isUploadingAvatar = false;
                this.accountService.getProfile().subscribe(data => {
                    this.avatarPreview = data.avatarUrl || null;
                    this.cdr.detectChanges();
                });
            })
        });
    }

    triggerLogoUpload(): void {
        const fileInput = document.getElementById('logoInput') as HTMLInputElement;
        fileInput?.click();
    }

    onLogoSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        if (!input.files || input.files.length === 0) return;

        const file = input.files[0];

        const validTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
        if (!validTypes.includes(file.type)) {
            this.toast.warning('Sai định dạng', 'Chỉ chấp nhận file ảnh (jpg, png, gif, webp)!');
            return;
        }

        if (file.size > 5 * 1024 * 1024) {
            this.toast.warning('Kích thước quá lớn', 'File không được vượt quá 5MB!');
            return;
        }

        const reader = new FileReader();
        reader.onload = (e) => this.ngZone.run(() => {
            this.logoPreview = e.target?.result as string;
            this.cdr.detectChanges();
        });
        reader.readAsDataURL(file);

        this.isUploadingLogo = true;
        const token = localStorage.getItem('authToken');
        const headers = new HttpHeaders({ 'Authorization': `Bearer ${token}` });

        const formData = new FormData();
        formData.append('file', file);

        this.http.post<{ url: string }>('/api/fileupload/avatar', formData, { headers }).subscribe({
            next: (res) => this.ngZone.run(() => {
                this.logoPreview = res.url;
                this.isUploadingLogo = false;
                this.cdr.detectChanges();
            }),
            error: (err) => this.ngZone.run(() => {
                console.error('Logo upload failed', err);
                this.toast.error('Tải lên thất bại', err.error?.message || 'Lỗi không xác định');
                this.isUploadingLogo = false;
                this.accountService.getCompanyInfo().subscribe(data => {
                    this.logoPreview = data.logoUrl || null;
                    this.cdr.detectChanges();
                });
            })
        });
    }

    saveSettings(): void {
        if (this.isLoading) return;
        this.isLoading = true;

        if (this.activeTab === 'account') {
            this.saveAccountSettings();
        } else if (this.activeTab === 'company') {
            this.saveCompanyInfo();
        } else if (this.activeTab === 'notifications') {
            this.saveNotificationSettings();
        } else if (this.activeTab === 'sla') {
            this.saveSlaSettings();
        }
    }

    canManageSla(): boolean {
        const normalizedRole = (this.userRole || '').toUpperCase();
        return normalizedRole === 'ADMIN' || normalizedRole === 'HR';
    }

    getSlaStageDisplayName(stage: SlaStageConfigDto): string {
        const code = (stage.code || '').toUpperCase();
        const nameMap: Record<string, string> = {
            NEW_APPLIED: 'Mới nộp',
            TEST_PASSED: 'Đã qua bài test',
            INTERVIEW: 'Phỏng vấn',
            OFFER: 'Đề nghị nhận việc',
            REJECTED: 'Từ chối',
            HIRED: 'Đã tuyển',
        };

        return nameMap[code] || stage.name;
    }

    getSlaStageCodeLabel(stage: SlaStageConfigDto): string {
        const code = (stage.code || '').toUpperCase();
        const codeMap: Record<string, string> = {
            NEW_APPLIED: 'MỚI_NỘP',
            TEST_PASSED: 'ĐÃ_QUA_TEST',
            INTERVIEW: 'PHỎNG_VẤN',
            OFFER: 'ĐỀ_NGHỊ',
            REJECTED: 'TỪ_CHỐI',
            HIRED: 'ĐÃ_TUYỂN',
        };

        return codeMap[code] || code;
    }

    onSlaEnabledChanged(stage: SlaStageConfigDto): void {
        if (!stage.isSlaEnabled || stage.isTerminal) return;

        if (!stage.slaMaxDays || stage.slaMaxDays < 1) {
            stage.slaMaxDays = 5;
        }

        if (stage.slaWarnBeforeDays == null || stage.slaWarnBeforeDays < 0) {
            stage.slaWarnBeforeDays = 1;
        }

        this.normalizeSlaValues(stage);
    }

    onSlaValueBlur(stage: SlaStageConfigDto): void {
        if (!stage.isSlaEnabled || stage.isTerminal) return;
        this.normalizeSlaValues(stage);
    }

    updateProfile(): void {
        if (this.accountForm.invalid) return;
        this.isLoading = true;

        const formValue = this.accountForm.value;
        const updateData = {
            fullName: formValue.fullName,
            phone: formValue.phone,
            avatarUrl: this.avatarPreview || ''
        };

        this.accountService.updateProfile(updateData).subscribe({
            next: () => {
                this.toast.success('Thành công', 'Cập nhật thông tin thành công!');
                this.isLoading = false;
            },
            error: (err: any) => {
                this.toast.error('Cập nhật thất bại', err.error?.message || 'Lỗi không xác định');
                this.isLoading = false;
            }
        });
    }

    saveAccountSettings(): void {
        if (this.passwordFormGroup.value.oldPassword || this.passwordFormGroup.value.newPassword) {
            // Validate
            if (this.passwordFormGroup.value.newPassword !== this.passwordFormGroup.value.confirmPassword) {
                this.toast.warning('Lỗi nhập liệu', 'Mật khẩu xác nhận không khớp!');
                return;
            }

            this.isLoading = true;
            this.authService.changePassword({
                currentPassword: this.passwordFormGroup.value.oldPassword,
                newPassword: this.passwordFormGroup.value.newPassword,
                confirmPassword: this.passwordFormGroup.value.confirmPassword
            }).subscribe({
                next: () => {
                    this.passwordFormGroup.reset();
                    this.toast.success('Thành công', 'Đổi mật khẩu thành công!');
                    // Tiếp tục cập nhật profile
                    this.updateProfile();
                },
                error: (err: any) => {
                    let errorMsg = 'Lỗi không xác định';
                    if (typeof err.error === 'string') {
                        errorMsg = err.error;
                    } else if (err.error?.message) {
                        errorMsg = err.error.message;
                    } else if (err.error?.errors) {
                        errorMsg = Object.values(err.error.errors).flat().join('\n');
                    }
                    this.toast.error('Đổi mật khẩu thất bại', errorMsg);
                    this.isLoading = false;
                }
            });
        } else {
            this.updateProfile();
        }
    }

    saveCompanyInfo(): void {
        if (this.companyForm.invalid) {
            this.toast.warning('Thiếu thông tin', 'Vui lòng nhập Tên Công Ty (bắt buộc)!');
            return;
        }
        this.isLoading = true;
        const formValue = this.companyForm.value;
        const submitData = {
            ...formValue,
            logoUrl: this.logoPreview || ''
        };

        this.accountService.updateCompanyInfo(submitData).subscribe({
            next: () => {
                this.toast.success('Thành công', 'Cập nhật thông tin công ty thành công!');
                this.isLoading = false;
            },
            error: (err: any) => {
                let errorMsg = 'Lỗi không xác định';
                if (typeof err.error === 'string') {
                    errorMsg = err.error;
                } else if (err.error?.message) {
                    errorMsg = err.error.message;
                } else if (err.error?.errors) {
                    errorMsg = Object.values(err.error.errors).flat().join('\n');
                }
                this.toast.error('Cập nhật thất bại', errorMsg);
                this.isLoading = false;
            }
        });
    }

    private saveNotificationSettings(): void {
        this.notificationDto.notifyApplicationUpdates = this.notificationUi.emailNewApplicant;
        this.notificationDto.notifyJobOpportunities = this.notificationUi.emailInterviewReminder;
        this.notificationDto.channelPush = this.notificationUi.systemAlerts;
        this.notificationDto.channelEmail = this.notificationUi.emailNewApplicant || this.notificationUi.emailInterviewReminder;

        this.accountService.updateNotificationSettings(this.notificationDto).subscribe({
            next: () => {
                this.toast.success('Đã lưu', 'Cấu hình thông báo đã được lưu!');
                this.isLoading = false;
            },
            error: (err: any) => {
                this.toast.error('Lỗi', 'Lưu cấu hình thất bại');
                this.isLoading = false;
            }
        });
    }

    private loadSlaConfigs(): void {
        this.isLoadingSla = true;
        this.applicationService.getSlaStageConfigs().subscribe({
            next: (res) => {
                this.slaStageConfigs = (res.data || []).sort((a, b) => a.sortOrder - b.sortOrder);
                this.isLoadingSla = false;
            },
            error: () => {
                this.slaStageConfigs = [];
                this.isLoadingSla = false;
            }
        });
    }

    private saveSlaSettings(): void {
        const editableStages = this.slaStageConfigs.filter(s => !s.isTerminal);

        if (!editableStages.length) {
            this.toast.warning('Không có dữ liệu', 'Không có stage nào để cấu hình SLA.');
            this.isLoading = false;
            return;
        }

        const invalidStage = editableStages.find(s =>
            s.isSlaEnabled && (
                !s.slaMaxDays ||
                s.slaMaxDays < 1 ||
                s.slaWarnBeforeDays == null ||
                s.slaWarnBeforeDays < 0 ||
                s.slaWarnBeforeDays > s.slaMaxDays
            )
        );

        if (invalidStage) {
            this.toast.warning(
                'Cấu hình SLA chưa hợp lệ',
                `Stage ${invalidStage.name}: Số ngày cảnh báo phải từ 0 đến ${invalidStage.slaMaxDays}.`
            );
            this.isLoading = false;
            return;
        }

        const requests = editableStages.map(stage => {
            const payload: UpdateSlaStageConfigRequest = {
                isSlaEnabled: stage.isSlaEnabled,
                slaMaxDays: stage.isSlaEnabled ? stage.slaMaxDays : undefined,
                slaWarnBeforeDays: stage.isSlaEnabled ? stage.slaWarnBeforeDays : undefined,
            };

            return this.applicationService.updateSlaStageConfig(stage.stageId, payload);
        });

        forkJoin(requests).subscribe({
            next: () => {
                this.toast.success('Thành công', 'Đã lưu cấu hình SLA theo stage.');
                this.isLoading = false;
                this.loadSlaConfigs();
            },
            error: (err: any) => {
                this.toast.error('Lưu thất bại', err?.error?.message || 'Không thể lưu cấu hình SLA.');
                this.isLoading = false;
            }
        });
    }

    private normalizeSlaValues(stage: SlaStageConfigDto): void {
        const maxDays = Number.isFinite(stage.slaMaxDays as number)
            ? Math.floor(stage.slaMaxDays as number)
            : 1;

        const warnDays = Number.isFinite(stage.slaWarnBeforeDays as number)
            ? Math.floor(stage.slaWarnBeforeDays as number)
            : 0;

        stage.slaMaxDays = Math.max(1, maxDays);
        stage.slaWarnBeforeDays = Math.min(stage.slaMaxDays, Math.max(0, warnDays));
    }
}

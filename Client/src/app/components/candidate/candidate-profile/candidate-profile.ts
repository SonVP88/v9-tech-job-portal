import { ChangeDetectorRef, Component, OnInit, NgZone, HostListener, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { take } from 'rxjs';
import { CandidateService, CandidateProfileDto, NotificationSettingDto, SkillDto } from '../../../services/candidate.service';
import { CandidateHeaderComponent } from '../../shared/candidate-header/candidate-header';
import { CandidateFooter } from '../../shared/candidate-footer/candidate-footer';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { ToastService } from '../../../services/toast.service';
import { AuthService } from '../../../services/auth.service';

@Component({
    selector: 'app-candidate-profile',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterModule, CandidateHeaderComponent, CandidateFooter],
    templateUrl: './candidate-profile.html',
    styleUrl: './candidate-profile.scss',
})
export class CandidateProfile implements OnInit {
    private authService = inject(AuthService);
    profileForm!: FormGroup;
    passwordForm!: FormGroup; // Added explicit declaration
    profile: CandidateProfileDto | null = null;
    cvList: Array<{
        id: string;
        name: string;
        displayName?: string;
        updated: string;
        size: string;
        url: string;
        isPrimary?: boolean;
        fileName?: string;
    }> = [];
    skills: SkillDto[] = [];
    activeCvMenuId: string | null = null;
    newSkillInput: string = '';

    // Autocomplete
    allSkills: SkillDto[] = [];
    filteredSkills: SkillDto[] = [];
    showSkillSuggestions = false;
    isLoading = false;
    loadError: string | null = null;
    isUploading = false;
    activeTab: 'general' | 'cv' | 'password' | 'notifications' = 'general';
    uploadingAvatar = false;
    isAvatarModalOpen = false;
    isSettingsLoading = false;
    isSettingsSaving = false;
    settingsLoadError: string | null = null;
    saveStatus: { message: string, type: 'success' | 'error' } | null = null;
    isProfileSaving = false;
    isPasswordChanging = false;

    // Password Visibility
    showCurrentPassword = false;
    showNewPassword = false;
    showConfirmPassword = false;

    // Password Strength
    passwordStrength = 0;
    passwordStrengthText = 'Yếu';
    passwordStrengthColor = 'bg-red-500';

    // Pagination
    currentPage = 1;
    itemsPerPage = 5;

    get paginatedCvList() {
        const startIndex = (this.currentPage - 1) * this.itemsPerPage;
        return this.cvList.slice(startIndex, startIndex + this.itemsPerPage);
    }

    get totalPages() {
        return Math.ceil(this.cvList.length / this.itemsPerPage);
    }

    changePage(page: number): void {
        if (page >= 1 && page <= this.totalPages) {
            this.currentPage = page;
        }
    }

    constructor(
        private fb: FormBuilder,
        private candidateService: CandidateService,
        private cdr: ChangeDetectorRef,
        private route: ActivatedRoute,
        private ngZone: NgZone,
        private toast: ToastService
    ) {
        this.initForm();
    }

    ngOnInit(): void {
        this.loadAllSkills();
        this.route.data.subscribe(data => {
            const profile = data['profile'];
            if (profile) {
                this.isLoading = false;
                this.updateProfileState(profile);
            } else {
                this.loadProfile();
            }
        });
    }

    loadAllSkills(): void {
        this.candidateService.getAllSkills().subscribe({
            next: (skills) => {
                this.allSkills = skills;
            },
            error: (err) => console.error('Failed to load skills list', err)
        });
    }

    onSkillInputChange(value: string): void {
        this.newSkillInput = value;
        if (value.trim().length < 1) {
            this.filteredSkills = [];
            this.showSkillSuggestions = false;
            return;
        }
        const normalizedQuery = value.trim().toUpperCase();
        this.filteredSkills = this.allSkills
            .filter(s => !this.skills.some(sk => sk.skillId === s.skillId))
            .filter(s => (s.normalizedName || s.name.toUpperCase()).includes(normalizedQuery))
            .slice(0, 8);
        this.showSkillSuggestions = this.filteredSkills.length > 0;
    }

    selectSuggestedSkill(skill: SkillDto): void {
        if (!this.skills.some(s => s.skillId === skill.skillId)) {
            this.skills.push(skill);
        }
        this.newSkillInput = '';
        this.filteredSkills = [];
        this.showSkillSuggestions = false;
    }

    hideSuggestions(): void {
        setTimeout(() => { this.showSkillSuggestions = false; }, 200);
    }

    initForm(): void {
        this.profileForm = this.fb.group({
            fullName: ['', [Validators.required, Validators.maxLength(200)]],
            phone: [''],
            location: ['', Validators.maxLength(300)],
            headline: ['', Validators.maxLength(200)],
            summary: ['', Validators.maxLength(2000)],
            linkedin: [''],
            github: ['']
        });

        this.passwordForm = this.fb.group({
            currentPassword: ['', Validators.required],
            newPassword: ['', [Validators.required, Validators.minLength(6)]],
            confirmPassword: ['', Validators.required],
            logoutOtherDevices: [false]
        }, { validators: this.passwordMatchValidator });
    }

    private updateProfileState(data: any): void {
        this.profile = data;
        this.profileForm.patchValue({
            fullName: data.fullName,
            phone: data.phone,
            location: data.location,
            headline: data.headline,
            summary: data.summary,
            linkedin: data.linkedIn,
            github: data.gitHub
        });

        this.skills = (data.skills || []).map((s: any) => ({
            skillId: s.skillId,
            name: s.skillName || s.name || ''
        }));
        this.cvList = (data.documents || []).map((doc: any) => ({
            id: doc.documentId,
            name: doc.fileName,
            displayName: doc.displayName,
            updated: new Date(doc.createdAt).toLocaleDateString('vi-VN'),
            size: this.formatFileSize(doc.sizeBytes || 0),
            url: doc.fileUrl,
            isPrimary: doc.isPrimary,
            fileName: doc.fileName
        }));
        this.cdr.detectChanges();
    }

    loadProfile(): void {
        this.isLoading = true;
        this.cdr.detectChanges();

        this.candidateService.getProfile().subscribe({
            next: (data) => {
                this.isLoading = false;
                this.updateProfileState(data);
            },
            error: (err) => {
                console.error('Failed to load profile', err);
                this.isLoading = false;
                this.loadError = 'Không thể tải thông tin hồ sơ.';
                this.cdr.detectChanges();
            }
        });
    }

    saveChanges(): void {
        // If user typed something in input but didn't press Enter, add it
        if (this.newSkillInput && this.newSkillInput.trim()) {
            this.addSkill();
        }

        if (this.profileForm.invalid) {
            this.profileForm.markAllAsTouched();
            return;
        }

        const formValue = this.profileForm.value;
        const updateDto = {
            fullName: formValue.fullName,
            phone: formValue.phone,
            location: formValue.location,
            headline: formValue.headline,
            summary: formValue.summary,
            linkedIn: formValue.linkedin,
            gitHub: formValue.github,
            skillIds: this.skills.map(s => s.skillId),
            skills: this.skills.map(s => s.name)
        };

        this.isProfileSaving = true;
        this.candidateService.updateProfile(updateDto).subscribe({
            next: (res) => {
                this.isProfileSaving = false;
                this.toast.success('Thành công', 'Cập nhật hồ sơ thành công!');
                this.cdr.detectChanges();
            },
            error: (err) => {
                this.isProfileSaving = false;
                this.toast.error('Thất bại', 'Cập nhật thất bại.');
                this.cdr.detectChanges();
            }
        });
    }

    onFileSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        if (input.files && input.files.length > 0) {
            this.uploadCV(input.files[0]);
        }
    }

    uploadCV(file: File): void {
        const allowedTypes = ['application/pdf', 'application/msword', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'];
        if (!allowedTypes.includes(file.type)) return;
        if (file.size > 10 * 1024 * 1024) return;

        this.isUploading = true;
        this.candidateService.uploadCV(file).subscribe({
            next: () => {
                this.isUploading = false;
                this.loadProfile();
            },
            error: (err) => {
                console.error('Upload failed', err);
                this.isUploading = false;
            }
        });
    }

    deleteCV(cvId: string): void {
        if (confirm('Bạn có chắc chắn muốn xóa CV này?')) {
            this.candidateService.deleteCV(cvId).subscribe({
                next: () => this.handleRefresh('Đã xóa CV thành công!'),
                error: (err) => console.error('Failed to delete CV', err)
            });
        }
    }

    addSkill(): void {
        const trimmed = this.newSkillInput.trim();
        if (!trimmed) return;

        // First, try to find exact match by normalizedName in allSkills
        const normalized = trimmed.toUpperCase();
        const existing = this.allSkills.find(s => (s.normalizedName || s.name.toUpperCase()) === normalized);

        if (existing && !this.skills.some(s => s.skillId === existing.skillId)) {
            this.skills.push(existing);
        } else if (!existing) {
            // For skills not in DB yet, create a temporary object - backend will upsert it
            if (!this.skills.some(s => s.name.toUpperCase() === normalized)) {
                this.skills.push({ skillId: '', name: trimmed });
            }
        }
        this.newSkillInput = '';
        this.filteredSkills = [];
        this.showSkillSuggestions = false;
    }

    removeSkill(index: number): void {
        this.skills.splice(index, 1);
    }

    formatFileSize(bytes: number): string {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
    }

    notificationSettings: NotificationSettingDto | null = null;

    loadNotificationSettings(): void {
        this.isSettingsLoading = true;
        this.cdr.detectChanges();

        this.candidateService.getNotificationSettings().pipe(take(1)).subscribe({
            next: (data) => {
                this.ngZone.run(() => {
                    this.notificationSettings = data;
                    this.isSettingsLoading = false;
                    this.cdr.detectChanges();
                });
            },
            error: (err) => {
                this.ngZone.run(() => {
                    this.isSettingsLoading = false;
                    this.settingsLoadError = 'Lỗi tải cài đặt.';
                    this.cdr.detectChanges();
                });
            }
        });
    }

    saveNotificationSettings(): void {
        if (!this.notificationSettings) return;
        this.isSettingsSaving = true;
        this.cdr.detectChanges();

        this.candidateService.updateNotificationSettings(this.notificationSettings).subscribe({
            next: (settings) => {
                this.ngZone.run(() => {
                    this.notificationSettings = settings;
                    this.isSettingsSaving = false;
                    this.saveStatus = { message: 'Đã lưu!', type: 'success' };
                    this.cdr.detectChanges();
                    setTimeout(() => { this.saveStatus = null; this.cdr.detectChanges(); }, 3000);
                });
            },
            error: () => {
                this.isSettingsSaving = false;
                this.saveStatus = { message: 'Lỗi lưu cài đặt.', type: 'error' };
                this.cdr.detectChanges();
            }
        });
    }

    switchTab(tab: 'general' | 'cv' | 'password' | 'notifications'): void {
        this.activeTab = tab;
        if (tab === 'notifications' && !this.notificationSettings) {
            this.loadNotificationSettings();
        }
        this.cdr.detectChanges();
    }

    onAvatarSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        if (input.files && input.files.length > 0) {
            this.uploadAvatar(input.files[0]);
        }
    }

    uploadAvatar(file: File): void {
        this.uploadingAvatar = true;
        this.candidateService.uploadAvatar(file).subscribe({
            next: (res) => {
                this.uploadingAvatar = false;
                if (this.profile && res.url) this.profile.avatar = res.url;
                this.toast.success('Thành công', 'Cập nhật avatar thành công!');
            },
            error: () => {
                this.uploadingAvatar = false;
                this.toast.error('Lỗi', 'Tải avatar thất bại.');
            }
        });
    }

    onPasswordInput(): void {
        const password = this.passwordForm.get('newPassword')?.value || '';
        let strength = 0;

        if (password.length >= 6) strength += 20;
        if (password.length >= 10) strength += 20;
        if (/[A-Z]/.test(password)) strength += 20;
        if (/[0-9]/.test(password)) strength += 20;
        if (/[^A-Za-z0-9]/.test(password)) strength += 20;

        this.passwordStrength = strength;

        if (strength <= 40) {
            this.passwordStrengthText = 'Yếu';
            this.passwordStrengthColor = 'bg-red-500';
        } else if (strength <= 80) {
            this.passwordStrengthText = 'Trung bình';
            this.passwordStrengthColor = 'bg-yellow-500';
        } else {
            this.passwordStrengthText = 'Mạnh';
            this.passwordStrengthColor = 'bg-green-500';
        }
        this.cdr.detectChanges();
    }

    private passwordMatchValidator(g: FormGroup) {
        return g.get('newPassword')?.value === g.get('confirmPassword')?.value
            ? null : { mismatch: true };
    }

    changePassword(): void {
        if (this.passwordForm.invalid) {
            this.passwordForm.markAllAsTouched();
            return;
        }

        this.isPasswordChanging = true;
        this.candidateService.changePassword(this.passwordForm.value)
            .subscribe({
                next: () => {
                    this.isPasswordChanging = false;
                    this.toast.success('Xong', 'Đã đổi mật khẩu.');
                    this.authService.logout();
                },
                error: (err) => {
                    this.isPasswordChanging = false;
                    this.toast.error('Lỗi', err.error?.message || 'Lỗi đổi mật khẩu');
                    this.cdr.detectChanges();
                }
            });
    }

    previewCV(cv: any): void {
        if (cv.url) window.open(cv.url, '_blank');
    }

    downloadCV(cv: any): void {
        if (cv.url) {
            this.candidateService.downloadFile(cv.url).subscribe({
                next: (blob) => {
                    const a = document.createElement('a');
                    const objectUrl = URL.createObjectURL(blob);
                    a.href = objectUrl;
                    a.download = cv.name || 'cv.pdf';
                    a.click();
                    URL.revokeObjectURL(objectUrl);
                },
                error: () => window.open(cv.url, '_blank')
            });
        }
    }

    handleRefresh(message: string): void {
        this.activeCvMenuId = null;
        this.loadProfile();
        setTimeout(() => this.toast.success('Xong', message), 100);
    }

    setPrimaryCV(cv: any): void {
        this.candidateService.setPrimaryDocument(cv.id).subscribe({
            next: () => this.handleRefresh('Đã đặt CV chính!'),
            error: () => { }
        });
    }

    openRenameModal(cv: any): void {
        const newName = prompt('Tên mới:', cv.displayName || cv.fileName);
        if (newName?.trim()) {
            this.candidateService.renameDocument(cv.id, newName.trim()).subscribe({
                next: () => this.handleRefresh('Đã đổi tên!'),
                error: () => this.toast.error('Lỗi', 'Không thể đổi tên.')
            });
        }
    }

    toggleCvMenu(cvId: string, event: Event): void {
        event.stopPropagation();
        this.activeCvMenuId = this.activeCvMenuId === cvId ? null : cvId;
    }

    viewAvatar(): void {
        this.isAvatarModalOpen = true;
    }

    closeAvatarModal(): void {
        this.isAvatarModalOpen = false;
    }

    @HostListener('document:click', ['$event'])
    onDocumentClick(event: MouseEvent) {
        this.activeCvMenuId = null;
    }
}

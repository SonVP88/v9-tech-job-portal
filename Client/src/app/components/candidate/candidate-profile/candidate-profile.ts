import { ChangeDetectorRef, Component, OnInit, NgZone, HostListener, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { take } from 'rxjs';
import { CandidateService, CandidateProfileDto, NotificationSettingDto } from '../../../services/candidate.service';
import { CandidateHeaderComponent } from '../../shared/candidate-header/candidate-header';
import { CandidateFooter } from '../../shared/candidate-footer/candidate-footer';
import { ActivatedRoute } from '@angular/router';
import { ToastService } from '../../../services/toast.service';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-candidate-profile',
  imports: [CommonModule, ReactiveFormsModule, FormsModule, CandidateHeaderComponent, CandidateFooter],
  templateUrl: './candidate-profile.html',
  styleUrl: './candidate-profile.scss',
})
export class CandidateProfile implements OnInit {
  private authService = inject(AuthService);
  profileForm!: FormGroup;
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
  skills: string[] = [];
  activeCvMenuId: string | null = null;
  newSkillInput: string = '';
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
    // Check for resolved data
    this.route.data.subscribe(data => {
      const profile = data['profile'];
      if (profile) {
        console.log(' Resolver data received:', profile);
        this.isLoading = false;
        try {
          this.profile = profile;
          this.profileForm.patchValue({
            fullName: profile.fullName,
            phone: profile.phone,
            location: profile.location,
            headline: profile.headline,
            summary: profile.summary,
            linkedin: profile.linkedIn,
            github: profile.gitHub
          });

          this.skills = (profile.skills || []).map((s: any) => s.skillName);

          this.cvList = (profile.documents || []).map((doc: any) => ({
            id: doc.documentId,
            name: doc.fileName,
            updated: new Date(doc.createdAt).toLocaleDateString('vi-VN'),
            size: this.formatFileSize(doc.sizeBytes || 0),
            url: doc.fileUrl
          }));
          this.cdr.detectChanges();
        } catch (e) {
          console.error('Error processing resolver data:', e);
        }
      } else {

        this.loadProfile();
      }
    });
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

  loadProfile(): void {
    // 1. Explicitly turn on loading
    this.isLoading = true;
    this.loadError = null;
    this.cdr.detectChanges(); // Update UI to show spinner

    this.candidateService.getProfile().subscribe({
      next: (data) => {
        console.log('Profile data received:', data);

        // 2. Turn off loading immediately when data arrives
        this.isLoading = false;

        try {
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

          // Load skills (check for null/undefined)
          this.skills = (data.skills || []).map(s => s.skillName);

          // Load CV documents (check for null/undefined)
          this.cvList = (data.documents || []).map((doc: any) => ({
            id: doc.documentId,
            name: doc.fileName || 'Tài liệu không tên', // Fallback name
            displayName: doc.displayName,
            updated: new Date(doc.createdAt).toLocaleDateString('vi-VN'),
            size: this.formatFileSize(doc.sizeBytes || 0),
            url: doc.fileUrl,
            isPrimary: doc.isPrimary,
            fileName: doc.fileName || 'Tài liệu chưa đặt tên' // Fallback
          }));

          this.currentPage = 1; // Reset pagination
        } catch (e) {
          console.error('Error processing profile data:', e);
          // Don't show full error screen if we have some data, just log it
        }

        // 3. Explicitly detect changes again to render data
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Failed to load profile', err);
        this.isLoading = false;
        this.loadError = 'Không thể tải thông tin hồ sơ. Vui lòng thử load lại trang.';
        this.cdr.detectChanges();
      }
    });
  }

  saveChanges(): void {
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
      skillIds: [], // Keep empty, we use skills names
      skills: this.skills // Send skill names to backend
    };

    this.isProfileSaving = true;
    this.candidateService.updateProfile(updateDto).subscribe({
      next: (res) => {
        this.isProfileSaving = false;
        // Show success feedback
        console.log('Profile updated successfully');
        this.toast.success('Thành công', 'Cập nhật hồ sơ thành công!');
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isProfileSaving = false;
        console.error('Failed to update profile', err);
        this.toast.error('Thất bại', 'Cập nhật thất bại. Vui lòng thử lại.');
        this.cdr.detectChanges();
      }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      this.uploadCV(file);
    }
  }

  uploadCV(file: File): void {
    // Validate file type
    const allowedTypes = ['application/pdf', 'application/msword', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'];
    if (!allowedTypes.includes(file.type)) {
      return;
    }

    // Validate file size (10MB)
    if (file.size > 10 * 1024 * 1024) {
      return;
    }

    this.isUploading = true;

    this.candidateService.uploadCV(file).subscribe({
      next: (response) => {
        this.isUploading = false;
        this.loadProfile(); // Reload to get updated CV list
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
        error: (err) => {
          console.error('Failed to delete CV', err);
          const errorMessage = err.error?.error || err.error?.message || err.statusText;
          this.toast.error('Không thể xóa', 'Chi tiết lỗi: ' + errorMessage);
        }
      });
    }
  }


  addSkill(): void {
    if (this.newSkillInput.trim()) {
      this.skills.push(this.newSkillInput.trim());
      this.newSkillInput = '';
    }
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

  // Notification Logic
  notificationSettings: NotificationSettingDto | null = null;

  loadNotificationSettings(): void {
    // 1. Explicitly turn on loading
    this.isSettingsLoading = true;
    this.settingsLoadError = null;
    this.cdr.detectChanges(); // Update UI to show spinner

    this.candidateService.getNotificationSettings().pipe(take(1)).subscribe({
      next: (data) => {
        console.log('📦 Notification settings received:', data);

        // 2. Use NgZone.run to ensure Angular catches the state change immediately
        this.ngZone.run(() => {
          this.notificationSettings = data;
          this.isSettingsLoading = false;
          this.cdr.markForCheck();
          this.cdr.detectChanges();
        });
      },
      error: (err) => {
        console.error(' Failed to load notification settings', err);
        this.ngZone.run(() => {
          this.isSettingsLoading = false;
          this.settingsLoadError = 'Không thể tải cài đặt thông báo. Vui lòng thử lại.';
          this.cdr.detectChanges();
        });
      }
    });
  }

  saveNotificationSettings(): void {
    if (!this.notificationSettings) return;

    this.isSettingsSaving = true;
    this.saveStatus = null;
    this.cdr.detectChanges();

    this.candidateService.updateNotificationSettings(this.notificationSettings).subscribe({
      next: (settings) => {
        this.ngZone.run(() => {
          this.notificationSettings = settings;
          this.isSettingsSaving = false;
          this.saveStatus = { message: 'Cài đặt đã được lưu thành công!', type: 'success' };
          this.cdr.detectChanges();

          // Auto-hide status after 3 seconds
          setTimeout(() => {
            this.saveStatus = null;
            this.cdr.detectChanges();
          }, 3000);
        });
      },
      error: (err) => {
        console.error('Failed to update notification settings', err);
        this.ngZone.run(() => {
          this.isSettingsSaving = false;
          this.saveStatus = { message: 'Cập nhật thất bại. Vui lòng thử lại.', type: 'error' };
          this.cdr.detectChanges();
        });
      }
    });
  }

  // Navigation Logic
  switchTab(tab: 'general' | 'cv' | 'password' | 'notifications'): void {
    this.activeTab = tab;

    if (tab === 'notifications') {
      // If we don't have settings, load them immediately
      // Pattern from job-search.ts: load immediately without complex delays
      if (!this.notificationSettings || this.settingsLoadError) {
        this.loadNotificationSettings();
      }
    }

    this.cdr.detectChanges(); // Ensure the tab DOM is rendered
  }

  // Avatar Logic
  onAvatarSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      this.uploadAvatar(file);
    }
  }

  uploadAvatar(file: File): void {
    // Validate file type
    const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
    if (!allowedTypes.includes(file.type)) {
      this.toast.warning('Sai định dạng file', 'Chỉ chấp nhận file ảnh (JPG, PNG, GIF, WEBP)');
      return;
    }

    // Validate size (5MB)
    if (file.size > 5 * 1024 * 1024) {
      this.toast.warning('File quá lớn', 'Kích thước ảnh không được quá 5MB');
      return;
    }

    this.uploadingAvatar = true;
    this.candidateService.uploadAvatar(file).subscribe({
      next: (res) => {
        this.uploadingAvatar = false;
        if (this.profile && res.url) {
          this.profile.avatar = res.url;
          console.log('Avatar updated:', res.url);
        }
        this.toast.success('Thành công', 'Cập nhật ảnh đại diện thành công!');
      },
      error: (err) => {
        console.error('Avatar upload failed', err);
        this.uploadingAvatar = false;
        this.toast.error('Lỗi tải lên', 'Vui lòng thử lại.');
      }
    });

    // Reset input
    // event target reset handled by *ngIf or manual reset if needed, mostly fine here
  }

  viewAvatar(): void {
    if (this.profile) {
      this.isAvatarModalOpen = true;
    }
  }

  closeAvatarModal(): void {
    this.isAvatarModalOpen = false;
  }

  downloadCV(cv: any): void {
    if (cv.url) {
      // Fix protocol and port if needed
      let url = cv.url;
      // 1. Handle relative paths (e.g. /uploads/... or uploads/...)
      // If it doesn't start with http, assume it's a relative path on the backend
      if (!url.startsWith('http')) {
        const cleanPath = url.startsWith('/') ? url.substring(1) : url;
        url = `https://localhost:7181/${cleanPath}`;
      }

      // 2. Fix protocol (http -> https) if we are on https
      if (url.startsWith('http://') && window.location.protocol === 'https:') {
        url = url.replace('http://', 'https://');
      }

      // 3. Fix localhost port if needed (5000 -> 7181)
      if (url.includes('localhost:5000')) {
        url = url.replace('localhost:5000', 'localhost:7181');
      }

      console.log('Downloading CV from URL:', url);

      // Use HttpClient to fetch as blob and force download
      this.candidateService.downloadFile(url).subscribe({
        next: (blob) => {
          // Check if blob is actually an error (JSON or HTML)
          if (blob.type.includes('application/json') || blob.type.includes('text/html')) {
            console.warn('Download returned JSON or HTML instead of binary. Falling back to window.open');
            window.open(url, '_blank');
            return;
          }

          const a = document.createElement('a');
          const objectUrl = URL.createObjectURL(blob);
          a.href = objectUrl;
          a.download = cv.name || 'cv_download.pdf'; // Use file name from CV object or default
          a.click();
          URL.revokeObjectURL(objectUrl);
        },
        error: (err) => {
          console.error('Download failed:', err);
          // Fallback to open in new tab if blob download fails (e.g. CORS issues)
          window.open(url, '_blank');
        }
      });
    } else {
      this.toast.error('Lỗi tải xuống', 'Không tìm thấy đường dẫn tải xuống cho CV này.');
    }
  }

  // Password Logic
  passwordForm!: FormGroup;

  showCurrentPassword = false;
  showNewPassword = false;
  showConfirmPassword = false;
  passwordStrength = 0;
  passwordStrengthText = '';
  passwordStrengthColor = 'bg-slate-200';

  passwordMatchValidator(g: FormGroup) {
    return g.get('newPassword')?.value === g.get('confirmPassword')?.value
      ? null : { mismatch: true };
  }

  onPasswordInput(): void {
    const password = this.passwordForm.get('newPassword')?.value || '';
    let strength = 0;

    if (password.length >= 6) strength += 20;
    if (password.match(/[a-z]+/)) strength += 20;
    if (password.match(/[A-Z]+/)) strength += 20;
    if (password.match(/[0-9]+/)) strength += 20;
    if (password.match(/[$@#&!]+/)) strength += 20;

    this.passwordStrength = strength;

    if (strength <= 20) {
      this.passwordStrengthText = 'Rất yếu';
      this.passwordStrengthColor = 'bg-red-500';
    } else if (strength <= 40) {
      this.passwordStrengthText = 'Yếu';
      this.passwordStrengthColor = 'bg-orange-500';
    } else if (strength <= 60) {
      this.passwordStrengthText = 'Trung bình';
      this.passwordStrengthColor = 'bg-yellow-500';
    } else if (strength <= 80) {
      this.passwordStrengthText = 'Mạnh';
      this.passwordStrengthColor = 'bg-blue-500';
    } else {
      this.passwordStrengthText = 'Rất mạnh';
      this.passwordStrengthColor = 'bg-green-500';
    }
  }

  changePassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    const { currentPassword, newPassword, confirmPassword, logoutOtherDevices } = this.passwordForm.value;

    this.isPasswordChanging = true;
    this.candidateService.changePassword({ currentPassword, newPassword, confirmPassword, logoutOtherDevices })
      .subscribe({
        next: (res) => {
          this.isPasswordChanging = false;
          this.toast.success('Thành công', 'Đổi mật khẩu thành công! Vui lòng đăng nhập lại.');
          // Sử dụng hàm Logout toàn cục
          this.authService.logout();
        },
        error: (err) => {
          this.isPasswordChanging = false;
          console.error(err);
          const errorMsg = typeof err.error === 'string' ? err.error : (err.error?.message || err.statusText);
          this.toast.error('Đổi mật khẩu thất bại', errorMsg);
          this.cdr.detectChanges();
        }
      });
  }

  // New Methods for CV Management

  previewCV(cv: any): void {
    if (cv.url) {
      // Fix URL protocol/port similar to download
      let url = cv.url;
      if (!url.startsWith('http')) {
        const cleanPath = url.startsWith('/') ? url.substring(1) : url;
        url = `https://localhost:7181/${cleanPath}`;
      }
      if (url.startsWith('http://') && window.location.protocol === 'https:') {
        url = url.replace('http://', 'https://');
      }
      if (url.includes('localhost:5000')) {
        url = url.replace('localhost:5000', 'localhost:7181');
      }
      window.open(url, '_blank');
    }
  }

  handleRefresh(message: string): void {
    this.activeCvMenuId = null;
    this.loadProfile();
    // setTimeout to allow loadProfile to start
    setTimeout(() => this.toast.success('Tuyệt vời!', message), 100);
  }

  setPrimaryCV(cv: any): void {
    if (confirm(`Đặt "${cv.displayName || cv.fileName}" làm CV chính?`)) {
      this.candidateService.setPrimaryDocument(cv.id).subscribe({
        next: () => this.handleRefresh('Đã đặt làm CV chính thành công!'),
        error: (err) => console.error('Error setting primary CV', err)
      });
    }
  }

  openRenameModal(cv: any): void {
    const newName = prompt('Nhập tên hiển thị mới:', cv.displayName || cv.fileName);
    if (newName && newName.trim() !== '') {
      this.candidateService.renameDocument(cv.id, newName.trim()).subscribe({
        next: (res: any) => this.handleRefresh(res.message || 'Đổi tên thành công!'),
        error: (err) => {
          console.error('Rename failed', err);
          if (err.status === 404) {
            this.toast.error('Lỗi tính năng', 'Backend chưa nhận diện được tính năng mới. Vui lòng khởi động lại server .NET.');
          } else {
            this.toast.error('Đổi tên thất bại', err.error?.message || 'Lỗi không xác định');
          }
        }
      });
    }
  }


  toggleCvMenu(cvId: string, event: Event): void {
    event.stopPropagation();
    this.activeCvMenuId = this.activeCvMenuId === cvId ? null : cvId;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    if (this.activeCvMenuId) {
      this.activeCvMenuId = null;
    }
  }
}

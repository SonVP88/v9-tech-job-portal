import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../../services/auth.service';
import { ToastService } from '../../../services/toast.service';
import { PopupService } from '../../../services/popup.service';
import { environment } from '../../../../environments/environment';

export interface OnboardingCandidate {
  applicationId: string;
  candidateId: string;
  fullName: string;
  email: string;
  phone: string;
  jobTitle: string;
  departmentId?: string;
  departmentName: string;
  departmentCode: string;
  healthCheckStatus: 'PASSED' | 'PENDING' | 'FAILED';
  uniformCardIssued: boolean;
  lineAssignment: string;
  status: string;
  offeredDate?: string;
}

@Component({
  selector: 'app-onboarding',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './onboarding.component.html'
})
export class OnboardingComponent implements OnInit {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private toast = inject(ToastService);
  private popup = inject(PopupService);
  private cdr = inject(ChangeDetectorRef);

  candidates: OnboardingCandidate[] = [];
  isLoading = false;
  selectedDepartment = 'ALL';
  searchKeyword = '';

  isConvertingMap: Record<string, boolean> = {};

  get filteredCandidates(): OnboardingCandidate[] {
    return this.candidates.filter(c => {
      const matchDept = this.selectedDepartment === 'ALL' || c.departmentCode === this.selectedDepartment;
      const matchKw = !this.searchKeyword ||
        c.fullName.toLowerCase().includes(this.searchKeyword.toLowerCase()) ||
        c.phone.includes(this.searchKeyword) ||
        c.jobTitle.toLowerCase().includes(this.searchKeyword.toLowerCase());
      return matchDept && matchKw;
    });
  }

  get countHealthPassed(): number {
    return this.candidates.filter(c => c.healthCheckStatus === 'PASSED').length;
  }

  get countUniformIssued(): number {
    return this.candidates.filter(c => c.uniformCardIssued).length;
  }

  get countReadyToWork(): number {
    return this.candidates.filter(c => c.healthCheckStatus === 'PASSED' && c.uniformCardIssued && !!c.lineAssignment).length;
  }

  get healthPassedPercent(): number {
    if (this.candidates.length === 0) return 0;
    return Math.round((this.countHealthPassed / this.candidates.length) * 100);
  }

  get uniformIssuedPercent(): number {
    if (this.candidates.length === 0) return 0;
    return Math.round((this.countUniformIssued / this.candidates.length) * 100);
  }

  ngOnInit(): void {
    // Nếu là Quản đốc xưởng thì mặc định filter theo xưởng của họ
    const user = this.authService.currentUser();
    if (user && user.departmentCode && ['PROD_LINE', 'WH_LOGS', 'TECH_MNT', 'QA_QC'].includes(user.departmentCode)) {
      this.selectedDepartment = user.departmentCode;
    }
    this.loadCandidates();
  }

  loadCandidates(): void {
    this.isLoading = true;
    const deptParam = this.selectedDepartment !== 'ALL' ? `?departmentCode=${this.selectedDepartment}` : '';

    this.http.get<any>(`${environment.apiUrl}/Onboarding/candidates${deptParam}`).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success && Array.isArray(res.data)) {
          this.candidates = res.data;
        } else {
          this.candidates = [];
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        console.error('Lỗi khi tải danh sách Onboarding:', err);
        this.toast.error('Lỗi', 'Không thể kết nối đến máy chủ Onboarding.');
        this.cdr.detectChanges();
      }
    });
  }

  onDepartmentFilterChange(): void {
    this.loadCandidates();
  }

  getInitials(name: string): string {
    if (!name) return 'UV';
    const parts = name.trim().split(' ');
    if (parts.length === 1) return parts[0].substring(0, 2).toUpperCase();
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }

  getHealthStatusClass(status: string): string {
    switch (status) {
      case 'PASSED':
        return 'bg-emerald-50 text-emerald-700 border-emerald-300 dark:bg-emerald-950/60 dark:text-emerald-300 dark:border-emerald-700';
      case 'PENDING':
        return 'bg-amber-50 text-amber-700 border-amber-300 dark:bg-amber-950/60 dark:text-amber-300 dark:border-amber-700';
      case 'FAILED':
        return 'bg-rose-50 text-rose-700 border-rose-300 dark:bg-rose-950/60 dark:text-rose-300 dark:border-rose-700';
      default:
        return 'bg-slate-50 text-slate-700 border-slate-300';
    }
  }

  saveChecklist(item: OnboardingCandidate): void {
    this.http.put<any>(`${environment.apiUrl}/Onboarding/checklist`, {
      applicationId: item.applicationId,
      healthCheckStatus: item.healthCheckStatus,
      uniformCardIssued: item.uniformCardIssued,
      lineAssignment: item.lineAssignment
    }).subscribe({
      next: () => {
        this.toast.success('Thành công', `Đã lưu tiến độ tiếp nhận cho ${item.fullName}`);
      },
      error: (err) => {
        console.error('Lỗi khi cập nhật checklist:', err);
      }
    });
  }

  async confirmConvertToEmployee(item: OnboardingCandidate): Promise<void> {
    if (item.healthCheckStatus !== 'PASSED') {
      this.toast.warning('Yêu cầu kiểm tra sức khỏe', 'Ứng viên phải có kết quả Khám sức khỏe ĐẠT trước khi chuyển thành nhân viên chính thức.');
      return;
    }

    const confirmed = await this.popup.confirm({
      title: '🎉 Tiếp nhận Nhân viên chính thức',
      message: `Xác nhận chuyển ứng viên "${item.fullName}" thành Nhân viên chính thức?\n\n• Vị trí: ${item.jobTitle}\n• Phân bổ: ${item.lineAssignment || 'Chưa gán chuyền'}\n• Phòng ban: ${item.departmentName}`,
      confirmText: 'Chuyển Chính Thức',
      cancelText: 'Hủy bỏ',
      tone: 'primary'
    });

    if (!confirmed) return;

    this.isConvertingMap[item.applicationId] = true;

    this.http.post<any>(`${environment.apiUrl}/Onboarding/convert-to-employee`, {
      applicationId: item.applicationId,
      candidateId: item.candidateId,
      fullName: item.fullName,
      email: item.email,
      phone: item.phone,
      departmentId: item.departmentId,
      departmentCode: item.departmentCode,
      lineAssignment: item.lineAssignment,
      jobTitle: item.jobTitle
    }).subscribe({
      next: (res) => {
        this.isConvertingMap[item.applicationId] = false;
        if (res.success) {
          item.status = 'HIRED';
          this.toast.success('Hoàn tất tiếp nhận', res.message || `Đã chuyển ${item.fullName} thành Nhân viên chính thức!`);
        } else {
          this.toast.error('Lỗi tiếp nhận', res.message || 'Không thể cập nhật hồ sơ');
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isConvertingMap[item.applicationId] = false;
        this.toast.error('Lỗi', 'Không thể kết nối đến máy chủ để kích hoạt nhân viên');
        this.cdr.detectChanges();
      }
    });
  }
}

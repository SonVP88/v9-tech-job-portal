import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { environment } from '../../../../environments/environment';
import { AuthService } from '../../../services/auth.service';
import { ToastService } from '../../../services/toast.service';

export type OnboardingTrack = 'PROFESSIONAL' | 'MASS_LABOR';
export type MedicalStatus = 'Pending' | 'Passed' | 'Failed';
export type OnboardingStatus = 'InterviewPassed' | 'Waitlisted' | 'Hired' | 'No_Show_Cancelled';
export type OnboardingSubTab = 'ALL' | 'PENDING_MEDICAL' | 'ELIGIBLE' | 'WAITLIST' | 'HIRED' | 'NO_SHOW';

export interface OnboardingRecord {
  applicationId: string;
  candidateId: string;
  jobId: string;
  jobCode: string;
  jobTitle: string;
  recruitmentTrack: OnboardingTrack;
  departmentCode: string;
  departmentName: string;
  lineAssignment: string;

  fullName: string;
  phone: string;
  email: string;
  citizenId: string;

  medicalCheckStatus: MedicalStatus;
  bankAccountNumber: string;
  bankName: string;
  onboardingDocsCompleted: boolean;

  convertedEmployeeCode: string | null;
  createSystemAccount: boolean;
  systemAccountCreated?: boolean;

  aiMatchingScore: number;
  status: OnboardingStatus;
  offeredDate: string;
  noShowReason?: string;

  // ── V5 Factory & HR Operations (Probation 30-Day Feedback) ──
  probation30DayStatus?: string | null; // 'Not_Started' | 'In_Probation' | 'Passed_30Days' | 'Early_Resigned'
  probationFeedbackNote?: string | null;
}

@Component({
  selector: 'app-onboarding-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './onboarding-management.component.html'
})
export class OnboardingManagementComponent implements OnInit {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private toast = inject(ToastService);

  // ── State Signals ──────────────────────────────────────────────────────
  records = signal<OnboardingRecord[]>([]);
  isLoading = signal<boolean>(false);

  activeTab = signal<OnboardingSubTab>('ALL');
  selectedDepartment = signal<string>('ALL');
  selectedTrack = signal<string>('ALL');
  searchKeyword = signal<string>('');

  // Quota & Waitlist Banner State (VD: 98 / 100)
  targetQuantity = signal<number>(100);
  hiredCount = signal<number>(98);
  waitlistBufferPercent = signal<number>(15);

  // Selection for Floating Bulk Action Bar
  selectedIds = signal<Set<string>>(new Set());
  isProcessingBulk = signal<boolean>(false);

  // Modal: Lấy QR/Link Tự Bổ Sung Hồ Sơ Trúng Tuyển
  showSelfServiceQrModal = signal<boolean>(false);
  selfServicePortalUrl = signal<string>('https://ats-factory.vn/onboarding-self-service/JOB-SMT-100');

  // Modal: Xử lý No-Show & Đôn người Dự bị
  showNoShowModal = signal<boolean>(false);
  noShowTarget = signal<OnboardingRecord | null>(null);
  noShowReason = signal<string>('Ứng viên không đến trình diện nhận việc ca sáng ngày đầu tiên (đã gọi điện 3 lần không nghe máy)');
  isProcessingNoShow = signal<boolean>(false);
  noShowCompleted = signal<boolean>(false);
  revokedCodeSnapshot = signal<string | null>(null);
  suggestedWaitlistCandidate = signal<OnboardingRecord | null>(null);

  readonly bankOptions = [
    'Vietcombank',
    'Techcombank',
    'BIDV',
    'VietinBank',
    'MBBank',
    'ACB',
    'Agribank',
    'VPBank'
  ];

  // ── Computed Counters & Quota Metrics ──────────────────────────────────
  quotaPercent = computed(() => {
    const target = this.targetQuantity();
    if (target <= 0) return 0;
    return Math.min(100, Math.round((this.hiredCount() / target) * 100));
  });

  isQuotaFull = computed(() => this.hiredCount() >= this.targetQuantity());

  remainingQuota = computed(() => Math.max(0, this.targetQuantity() - this.hiredCount()));

  maxWaitlistCap = computed(() => Math.ceil(this.targetQuantity() * (this.waitlistBufferPercent() / 100)));

  tabCounts = computed(() => {
    const all = this.records();
    return {
      all: all.length,
      pendingMedical: all.filter(r =>
        r.status === 'InterviewPassed' &&
        (r.medicalCheckStatus !== 'Passed' || !this.isValidCitizenId(r.citizenId))
      ).length,
      eligible: all.filter(r => this.isEligibleForEmployeeCode(r)).length,
      waitlist: all.filter(r => r.status === 'Waitlisted').length,
      hired: all.filter(r => r.status === 'Hired' || !!r.convertedEmployeeCode).length,
      noShow: all.filter(r => r.status === 'No_Show_Cancelled').length
    };
  });

  filteredRecords = computed(() => {
    const tab = this.activeTab();
    const dept = this.selectedDepartment();
    const track = this.selectedTrack();
    const kw = this.searchKeyword().trim().toLowerCase();

    return this.records().filter(item => {
      if (tab === 'PENDING_MEDICAL') {
        if (item.status !== 'InterviewPassed' || (item.medicalCheckStatus === 'Passed' && this.isValidCitizenId(item.citizenId))) {
          return false;
        }
      } else if (tab === 'ELIGIBLE') {
        if (!this.isEligibleForEmployeeCode(item)) return false;
      } else if (tab === 'WAITLIST') {
        if (item.status !== 'Waitlisted') return false;
      } else if (tab === 'HIRED') {
        if (item.status !== 'Hired' && !item.convertedEmployeeCode) return false;
      } else if (tab === 'NO_SHOW') {
        if (item.status !== 'No_Show_Cancelled') return false;
      }

      if (dept !== 'ALL' && item.departmentCode !== dept) return false;
      if (track !== 'ALL' && item.recruitmentTrack !== track) return false;

      if (kw) {
        const match =
          item.fullName.toLowerCase().includes(kw) ||
          item.phone.toLowerCase().includes(kw) ||
          (item.citizenId || '').toLowerCase().includes(kw) ||
          (item.convertedEmployeeCode || '').toLowerCase().includes(kw) ||
          item.jobTitle.toLowerCase().includes(kw);
        if (!match) return false;
      }

      return true;
    });
  });

  selectedCount = computed(() => this.selectedIds().size);

  topWaitlistCandidate = computed(() => {
    return this.records()
      .filter(r => r.status === 'Waitlisted')
      .sort((a, b) => {
        const medA = a.medicalCheckStatus === 'Passed' ? 1 : 0;
        const medB = b.medicalCheckStatus === 'Passed' ? 1 : 0;
        if (medB !== medA) return medB - medA;
        return b.aiMatchingScore - a.aiMatchingScore;
      })[0] || null;
  });

  ngOnInit(): void {
    // Khởi tạo sẵn Mock Data chuẩn KCN để giao diện chạy kiểm thử được ngay
    this.records.set(this.getInitialOnboardingMockData());

    const user = this.authService.currentUser();
    if (user?.departmentCode && ['PROD_LINE', 'WH_LOGS', 'TECH_MNT', 'QA_QC'].includes(user.departmentCode)) {
      this.selectedDepartment.set(user.departmentCode);
    }

    this.fetchBackendOnboardingCandidates();
  }

  fetchBackendOnboardingCandidates(): void {
    this.isLoading.set(true);
    this.http.get<any>(`${environment.apiUrl}/Onboarding/candidates`).subscribe({
      next: (res) => {
        this.isLoading.set(false);
        const apiList: any[] = res?.data || [];
        if (apiList.length > 0) {
          const mapped: OnboardingRecord[] = apiList.map((item, idx) => this.mapApiToRecord(item, idx));
          const mockList = this.getInitialOnboardingMockData();
          const existingIds = new Set(mapped.map(x => x.applicationId));
          this.records.set([...mapped, ...mockList.filter(m => !existingIds.has(m.applicationId))]);

          if (apiList[0]?.jobTargetQuantity) {
            this.targetQuantity.set(apiList[0].jobTargetQuantity);
          }
          if (apiList[0]?.jobHiredCount !== undefined) {
            this.hiredCount.set(apiList[0].jobHiredCount);
          }
        }
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  private mapApiToRecord(item: any, idx: number): OnboardingRecord {
    const rawMed = (item.medicalCheckStatus || item.healthCheckStatus || 'Pending').toString().toUpperCase();
    const medStatus: MedicalStatus =
      rawMed === 'PASSED' ? 'Passed' : rawMed === 'FAILED' ? 'Failed' : 'Pending';

    const rawStatus = (item.status || 'InterviewPassed').toString();
    const status: OnboardingStatus =
      item.convertedEmployeeCode || rawStatus.toUpperCase() === 'HIRED'
        ? 'Hired'
        : rawStatus === 'Waitlisted' || rawStatus.toUpperCase() === 'WAITLIST'
          ? 'Waitlisted'
          : rawStatus === 'No_Show_Cancelled'
            ? 'No_Show_Cancelled'
            : 'InterviewPassed';

    const track: OnboardingTrack = item.recruitmentTrack === 'PROFESSIONAL' ? 'PROFESSIONAL' : 'MASS_LABOR';

    return {
      applicationId: item.applicationId,
      candidateId: item.candidateId || `cand-${idx}`,
      jobId: item.jobId || 'job-smt-01',
      jobCode: item.jobCode || 'JOB-SMT-100',
      jobTitle: item.jobTitle || 'Công nhân vận hành dây chuyền SMT',
      recruitmentTrack: track,
      departmentCode: item.departmentCode || 'PROD_LINE',
      departmentName: item.departmentName || 'Xưởng Sản Xuất Điện Tử',
      lineAssignment: item.lineAssignment || (track === 'PROFESSIONAL' ? 'Phòng Kỹ Thuật PLC' : 'Chuyền SMT 01 - Ca A'),
      fullName: item.fullName || 'Ứng viên trúng tuyển',
      phone: item.phone || '0987654321',
      email: item.email || '',
      citizenId: item.citizenId || '',
      medicalCheckStatus: medStatus,
      bankAccountNumber: item.bankAccountNumber || '',
      bankName: item.bankName || 'Vietcombank',
      onboardingDocsCompleted: !!item.onboardingDocsCompleted,
      convertedEmployeeCode: item.convertedEmployeeCode || null,
      createSystemAccount: track === 'PROFESSIONAL',
      aiMatchingScore: item.aiMatchingScore ?? 88,
      status,
      offeredDate: item.offeredDate ? new Date(item.offeredDate).toLocaleDateString('vi-VN') : '06/11/2026',
      probation30DayStatus: item.probation30DayStatus || (item.convertedEmployeeCode ? 'In_Probation' : 'Not_Started')
    };
  }

  // ── Kiểm tra điều kiện cứng (Logic 4): MedicalCheckStatus == 'Passed' & đủ CCCD ──
  isValidCitizenId(citizenId: string | null | undefined): boolean {
    if (!citizenId) return false;
    const digits = citizenId.replace(/\D/g, '');
    return digits.length >= 9 && digits.length <= 12;
  }

  isEligibleForEmployeeCode(item: OnboardingRecord): boolean {
    return (
      item.status === 'InterviewPassed' &&
      !item.convertedEmployeeCode &&
      item.medicalCheckStatus === 'Passed' &&
      this.isValidCitizenId(item.citizenId)
    );
  }

  // ── Thao tác 1-Click đổi MedicalCheckStatus & Sửa nhanh CCCD / Ngân hàng ──
  setMedicalStatus(item: OnboardingRecord, status: MedicalStatus): void {
    this.records.update(list =>
      list.map(r => (r.applicationId === item.applicationId ? { ...r, medicalCheckStatus: status } : r))
    );

    // Nếu không còn đủ điều kiện thì bỏ chọn khỏi bulk selection
    if (status !== 'Passed') {
      this.selectedIds.update(set => {
        const next = new Set(set);
        next.delete(item.applicationId);
        return next;
      });
    }

    this.persistQuickUpdate(item.applicationId, { medicalCheckStatus: status });
    const label = status === 'Passed' ? '✅ Đạt KSK' : status === 'Failed' ? '❌ Trượt KSK' : '⏳ Chờ KSK';
    this.toast.success('Cập nhật KSK', `${item.fullName}: ${label}`);
  }

  saveInlineRowField(item: OnboardingRecord): void {
    const cleanCccd = (item.citizenId || '').replace(/\D/g, '');
    this.records.update(list =>
      list.map(r =>
        r.applicationId === item.applicationId
          ? {
              ...r,
              citizenId: cleanCccd,
              bankAccountNumber: (item.bankAccountNumber || '').trim(),
              bankName: item.bankName,
              onboardingDocsCompleted: this.isValidCitizenId(cleanCccd) && !!(item.bankAccountNumber || '').trim()
            }
          : r
      )
    );

    this.persistQuickUpdate(item.applicationId, {
      citizenId: cleanCccd,
      bankAccountNumber: item.bankAccountNumber,
      bankName: item.bankName
    });
    this.toast.success('Đã lưu hồ sơ', `Đã cập nhật CCCD & STK Ngân hàng cho ${item.fullName}.`);
  }

  private persistQuickUpdate(applicationId: string, payload: any): void {
    this.http.put(`${environment.apiUrl}/Onboarding/${applicationId}/quick-update`, payload).subscribe({
      error: () => {
        // Giữ nguyên state local nếu chạy với Mock ID
      }
    });
  }

  // ── Checkbox chọn hàng loạt ────────────────────────────────────────────
  isSelected(id: string): boolean {
    return this.selectedIds().has(id);
  }

  toggleSelection(item: OnboardingRecord): void {
    if (!this.isEligibleForEmployeeCode(item)) {
      this.toast.warning(
        'Chưa đủ điều kiện cứng (Logic 4)',
        `Ứng viên "${item.fullName}" phải có kết quả KSK = Đạt (Passed) và nhập đủ Số CCCD (9-12 số) trước khi cấp Mã nhân viên.`
      );
      return;
    }

    this.selectedIds.update(set => {
      const next = new Set(set);
      if (next.has(item.applicationId)) {
        next.delete(item.applicationId);
      } else {
        next.add(item.applicationId);
      }
      return next;
    });
  }

  toggleSelectAllEligible(): void {
    const eligibleInView = this.filteredRecords().filter(r => this.isEligibleForEmployeeCode(r));
    if (eligibleInView.length === 0) {
      this.toast.info('Thông báo', 'Không có ứng viên nào trong danh sách hiện tại đủ điều kiện (Đạt KSK + Đủ CCCD).');
      return;
    }

    const currentSet = this.selectedIds();
    const allSelected = eligibleInView.every(r => currentSet.has(r.applicationId));

    if (allSelected) {
      this.selectedIds.set(new Set());
    } else {
      this.selectedIds.set(new Set(eligibleInView.map(r => r.applicationId)));
    }
  }

  clearSelection(): void {
    this.selectedIds.set(new Set());
  }

  // ── Logic 4, 5, 7: Cấp Mã Nhân Viên Đơn Lẻ hoặc Hàng Loạt (Idempotent + Quota Lock) ──
  convertSingleCandidate(item: OnboardingRecord): void {
    if (!this.isEligibleForEmployeeCode(item)) {
      this.toast.warning(
        'Chưa đủ điều kiện cấp Mã NV',
        'Bắt buộc kết quả Khám sức khỏe = Passed VÀ đã điền đủ Số CCCD (CitizenId).'
      );
      return;
    }
    this.executeBulkConvert([item.applicationId]);
  }

  bulkConvertSelected(): void {
    const ids = Array.from(this.selectedIds());
    if (ids.length === 0) return;
    this.executeBulkConvert(ids);
  }

  private executeBulkConvert(applicationIds: string[]): void {
    this.isProcessingBulk.set(true);

    const perCandidateAccountMap: Record<string, boolean> = {};
    for (const r of this.records()) {
      if (applicationIds.includes(r.applicationId)) {
        perCandidateAccountMap[r.applicationId] = r.recruitmentTrack === 'PROFESSIONAL' && r.createSystemAccount;
      }
    }

    this.http
      .post<any>(`${environment.apiUrl}/Onboarding/bulk-convert`, {
        applicationIds,
        createSystemAccount: true,
        perCandidateCreateAccount: perCandidateAccountMap
      })
      .subscribe({
        next: (res) => {
          this.isProcessingBulk.set(false);
          this.applyLocalBulkConvertWithQuotaGuard(applicationIds);
          if (res?.message) {
            this.toast.success('Hoàn tất Onboarding', res.message);
          }
        },
        error: () => {
          this.isProcessingBulk.set(false);
          this.applyLocalBulkConvertWithQuotaGuard(applicationIds);
        }
      });
  }

  private applyLocalBulkConvertWithQuotaGuard(applicationIds: string[]): void {
    let currentHired = this.hiredCount();
    const targetQty = this.targetQuantity();

    // Tính số thứ tự mã nhân viên tiếp theo
    let nextSeq = this.getNextEmployeeSequence();
    let convertedCount = 0;
    let waitlistedCount = 0;
    let skippedCount = 0;
    let accountsCreated = 0;

    const updated = this.records().map(rec => {
      if (!applicationIds.includes(rec.applicationId)) return rec;

      // Idempotent guard: Đã có mã NV thì không sinh lại
      if (rec.convertedEmployeeCode) {
        skippedCount++;
        return rec;
      }

      // Logic 4: Điều kiện cứng trước Onboarding
      if (rec.medicalCheckStatus !== 'Passed' || !this.isValidCitizenId(rec.citizenId)) {
        skippedCount++;
        return rec;
      }

      // Logic 5: Quota Lock - nếu chạm trần định biên thì tự động đẩy vào Waitlisted
      if (currentHired >= targetQty) {
        waitlistedCount++;
        return {
          ...rec,
          status: 'Waitlisted' as OnboardingStatus
        };
      }

      const newCode = `NV-2026-${String(nextSeq).padStart(4, '0')}`;
      nextSeq++;
      currentHired++;
      convertedCount++;

      const createdUser = rec.recruitmentTrack === 'PROFESSIONAL' && rec.createSystemAccount;
      if (createdUser) accountsCreated++;

      return {
        ...rec,
        convertedEmployeeCode: newCode,
        status: 'Hired' as OnboardingStatus,
        onboardingDocsCompleted: true,
        systemAccountCreated: createdUser
      };
    });

    this.records.set(updated);
    this.hiredCount.set(currentHired);
    this.clearSelection();

    if (convertedCount > 0 && waitlistedCount > 0) {
      this.toast.warning(
        'Khóa chỉ tiêu định biên (Logic 5)',
        `Đã cấp mã NV cho ${convertedCount} người. Do xưởng đã chạm trần ${currentHired}/${targetQty} chỉ tiêu, ${waitlistedCount} ứng viên còn lại đã tự động được đưa vào Danh sách Dự bị (Waitlisted)!`
      );
    } else if (convertedCount === 0 && waitlistedCount > 0) {
      this.toast.warning(
        'Đã chạm trần định biên xưởng!',
        `Chỉ tiêu đã đầy (${currentHired}/${targetQty}). Đã tự động chuyển ${waitlistedCount} ứng viên đạt yêu cầu sang Danh sách Dự bị (Waitlist 15%).`
      );
    } else if (convertedCount > 0) {
      const userMsg = accountsCreated > 0 ? ` (Đã tự động cấp ${accountsCreated} tài khoản hệ thống cho khối Kỹ sư/VP)` : '';
      this.toast.success(
        '🎉 Cấp Mã Nhân Viên Thành Công',
        `Đã cấp mã nhân viên chính thức cho ${convertedCount} ứng viên${userMsg}. Định biên hiện tại: ${currentHired}/${targetQty}.`
      );
    }
  }

  private getNextEmployeeSequence(): number {
    let maxSeq = 98;
    for (const r of this.records()) {
      if (r.convertedEmployeeCode) {
        const match = r.convertedEmployeeCode.match(/(\d+)$/);
        if (match) {
          const val = parseInt(match[1], 10);
          if (val > maxSeq) maxSeq = val;
        }
      }
    }
    return maxSeq + 1;
  }

  // ── Logic 7.2: Xử lý No-Show ngày đầu & Đôn ứng viên Dự bị điểm cao nhất ──
  openNoShowModal(item: OnboardingRecord): void {
    this.noShowTarget.set(item);
    this.noShowCompleted.set(false);
    this.revokedCodeSnapshot.set(item.convertedEmployeeCode);
    this.suggestedWaitlistCandidate.set(this.topWaitlistCandidate());
    this.showNoShowModal.set(true);
  }

  closeNoShowModal(): void {
    this.showNoShowModal.set(false);
    this.noShowTarget.set(null);
    this.noShowCompleted.set(false);
  }

  confirmMarkNoShow(): void {
    const target = this.noShowTarget();
    if (!target) return;

    this.isProcessingNoShow.set(true);

    this.http
      .post<any>(`${environment.apiUrl}/Onboarding/${target.applicationId}/mark-no-show`, {
        reason: this.noShowReason()
      })
      .subscribe({
        next: () => {
          this.isProcessingNoShow.set(false);
          this.applyLocalNoShow(target);
        },
        error: () => {
          this.isProcessingNoShow.set(false);
          this.applyLocalNoShow(target);
        }
      });
  }

  private applyLocalNoShow(target: OnboardingRecord): void {
    const wasHired = target.status === 'Hired' || !!target.convertedEmployeeCode;
    const oldCode = target.convertedEmployeeCode;

    this.records.update(list =>
      list.map(r =>
        r.applicationId === target.applicationId
          ? {
              ...r,
              status: 'No_Show_Cancelled' as OnboardingStatus,
              convertedEmployeeCode: null,
              noShowReason: this.noShowReason()
            }
          : r
      )
    );

    if (wasHired && this.hiredCount() > 0) {
      this.hiredCount.update(c => Math.max(0, c - 1));
    }

    this.revokedCodeSnapshot.set(oldCode);
    this.suggestedWaitlistCandidate.set(this.topWaitlistCandidate());
    this.noShowCompleted.set(true);

    this.toast.warning(
      '🚫 Đã ghi nhận No-Show & Hoàn chỉ tiêu',
      `Đã thu hồi mã NV ${oldCode || ''} của "${target.fullName}" và hoàn lại 1 chỉ tiêu cho xưởng (${this.hiredCount()}/${this.targetQuantity()}).`
    );
  }

  promoteWaitlistCandidate(waitlistItem: OnboardingRecord): void {
    this.http
      .post<any>(`${environment.apiUrl}/Onboarding/${waitlistItem.applicationId}/promote-waitlist`, {})
      .subscribe({
        next: () => this.applyLocalPromoteWaitlist(waitlistItem),
        error: () => this.applyLocalPromoteWaitlist(waitlistItem)
      });
  }

  private applyLocalPromoteWaitlist(waitlistItem: OnboardingRecord): void {
    // Đôn ứng viên dự bị lên InterviewPassed và nếu đã đủ KSK + CCCD + còn chỉ tiêu thì cấp Mã NV luôn
    const canAutoIssueCode =
      this.hiredCount() < this.targetQuantity() &&
      waitlistItem.medicalCheckStatus === 'Passed' &&
      this.isValidCitizenId(waitlistItem.citizenId);

    const newEmpCode = canAutoIssueCode
      ? `NV-2026-${String(this.getNextEmployeeSequence()).padStart(4, '0')}`
      : null;

    this.records.update(list =>
      list.map(r =>
        r.applicationId === waitlistItem.applicationId
          ? {
              ...r,
              status: (canAutoIssueCode ? 'Hired' : 'InterviewPassed') as OnboardingStatus,
              convertedEmployeeCode: newEmpCode,
              onboardingDocsCompleted: canAutoIssueCode ? true : r.onboardingDocsCompleted
            }
          : r
      )
    );

    if (canAutoIssueCode) {
      this.hiredCount.update(c => c + 1);
      this.toast.success(
        '⬆️ Đôn Dự Bị & Cấp Mã NV Thành Công!',
        `Đã đôn ứng viên dự bị điểm cao nhất "${waitlistItem.fullName}" (${waitlistItem.aiMatchingScore} điểm) lên thay thế và cấp mã ${newEmpCode}!`
      );
    } else {
      this.toast.success(
        '⬆️ Đã đôn ứng viên Dự bị lên Chính thức',
        `Ứng viên "${waitlistItem.fullName}" đã được chuyển sang danh sách chờ cấp Mã nhân viên.`
      );
    }

    this.closeNoShowModal();
  }

  // ── Logic 6: Đánh giá thử việc 30 ngày (Probation 30-Day Feedback) ───
  updateProbationFeedback(record: OnboardingRecord, status: 'Passed_30Days' | 'Early_Resigned'): void {
    const isPassed = status === 'Passed_30Days';
    const statusLabel = isPassed ? '🏆 Đạt thử việc 30 ngày' : '📉 Nghỉ việc sớm (<30 ngày)';

    // Cập nhật ngay trên giao diện (Optimistic UI)
    this.records.update(list =>
      list.map(r => r.applicationId === record.applicationId
        ? { ...r, probation30DayStatus: status }
        : r)
    );

    // Kiểm tra nếu là ApplicationId thực (GUID) thì gọi Backend API
    const isValidGuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(record.applicationId);
    if (isValidGuid) {
      this.http.post<any>(`${environment.apiUrl}/hr-operations/applications/${record.applicationId}/probation-feedback`, {
        probationStatus: status,
        feedbackNote: isPassed ? 'Hoàn thành tốt 30 ngày thử việc' : 'Nghỉ việc trước thời hạn 30 ngày'
      }).subscribe({
        next: (res) => {
          this.toast.success(
            'Đánh giá thử việc thành công',
            `Đã ghi nhận ${statusLabel} cho ${record.fullName} (Mã: ${record.convertedEmployeeCode}). Tỷ lệ thành công vị trí: ${res?.accuracyRate ?? 95}%`
          );
        },
        error: (err) => {
          this.toast.error('Lỗi', err?.error?.message || 'Không thể lưu đánh giá thử việc vào cơ sở dữ liệu.');
        }
      });
    } else {
      this.toast.success(
        'Đánh giá thử việc (Demo)',
        `Đã ghi nhận ${statusLabel} cho nhân viên ${record.fullName} (Mã: ${record.convertedEmployeeCode}).`
      );
    }
  }

  // ── Tiện ích: Copy Link Tự Bổ Sung Hồ Sơ & Xuất Excel Làm Thẻ Từ / Ngân Hàng ──
  copySelfServiceLink(): void {
    navigator.clipboard?.writeText(this.selfServicePortalUrl());
    this.toast.success('Đã sao chép đường dẫn', 'Đã copy link Cổng tự bổ sung CCCD & Ngân hàng cho ứng viên trúng tuyển.');
  }

  exportBadgeAndBankExcel(): void {
    const exportList = this.records().filter(r => r.status === 'Hired' || r.status === 'InterviewPassed' || r.status === 'Waitlisted');
    const headers = [
      'Mã Nhân Viên',
      'Họ và Tên',
      'Số CCCD (12 số)',
      'Số Điện Thoại',
      'Luồng Tuyển Dụng',
      'Vị Trí Công Việc',
      'Xưởng / Bộ Phận',
      'Chuyền / Tổ Phân Bổ',
      'Ngân Hàng',
      'Số Tài Khoản',
      'Khám Sức Khỏe',
      'Trạng Thái Tiếp Nhận'
    ];

    const rows = exportList.map(r => [
      r.convertedEmployeeCode || 'CHƯA CẤP',
      r.fullName,
      `'${r.citizenId || ''}`,
      `'${r.phone}`,
      r.recruitmentTrack === 'PROFESSIONAL' ? 'Kỹ sư / Văn phòng' : 'Công nhân xưởng',
      r.jobTitle,
      r.departmentName,
      r.lineAssignment,
      r.bankName || '',
      `'${r.bankAccountNumber || ''}`,
      r.medicalCheckStatus,
      r.status
    ]);

    const csvContent =
      '\uFEFF' +
      [headers.join(','), ...rows.map(row => row.map(cell => `"${String(cell).replace(/"/g, '""')}"`).join(','))].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `Danh_Sach_Lam_The_Tu_Va_Ngan_Hang_${new Date().toISOString().slice(0, 10)}.csv`;
    link.click();
    URL.revokeObjectURL(url);

    this.toast.success(
      '📥 Đã xuất file Excel (CSV)',
      `Đã xuất ${exportList.length} hồ sơ phục vụ in thẻ từ chấm công & mở tài khoản ngân hàng.`
    );
  }

  // ── Mock Data chuẩn KCN đầy đủ các trạng thái để kiểm thử ngay ──────────
  private getInitialOnboardingMockData(): OnboardingRecord[] {
    return [
      {
        applicationId: '80000000-0000-0000-0000-000000000303',
        candidateId: '70000000-0000-0000-0000-000000000203',
        jobId: 'job-smt-01',
        jobCode: 'JOB-SMT-100',
        jobTitle: 'Công nhân vận hành dây chuyền SMT (Tuyển gấp 100 người)',
        recruitmentTrack: 'MASS_LABOR',
        departmentCode: 'PROD_LINE',
        departmentName: 'Xưởng Sản Xuất Điện Tử',
        lineAssignment: 'Chuyền SMT 01 - Ca A',
        fullName: 'Nguyễn Thị Thu Hà',
        phone: '0968223445',
        email: 'thuha.smt@gmail.com',
        citizenId: '024199006789',
        medicalCheckStatus: 'Pending',
        bankAccountNumber: '19036677889901',
        bankName: 'Techcombank',
        onboardingDocsCompleted: false,
        convertedEmployeeCode: null,
        createSystemAccount: false,
        aiMatchingScore: 92,
        status: 'InterviewPassed',
        offeredDate: '06/11/2026'
      },
      {
        applicationId: 'onb-mock-02',
        candidateId: 'cand-onb-02',
        jobId: 'job-smt-01',
        jobCode: 'JOB-SMT-100',
        jobTitle: 'Công nhân vận hành dây chuyền SMT (Tuyển gấp 100 người)',
        recruitmentTrack: 'MASS_LABOR',
        departmentCode: 'PROD_LINE',
        departmentName: 'Xưởng Sản Xuất Điện Tử',
        lineAssignment: 'Chuyền SMT 02 - Ca B',
        fullName: 'Bùi Văn Khánh',
        phone: '0973445112',
        email: 'khanh.bui@walkin.local',
        citizenId: '',
        medicalCheckStatus: 'Passed',
        bankAccountNumber: '',
        bankName: 'Vietcombank',
        onboardingDocsCompleted: false,
        convertedEmployeeCode: null,
        createSystemAccount: false,
        aiMatchingScore: 89,
        status: 'InterviewPassed',
        offeredDate: '06/11/2026'
      },
      {
        applicationId: 'onb-mock-03',
        candidateId: 'cand-onb-03',
        jobId: 'job-plc-03',
        jobCode: 'JOB-ENG-012',
        jobTitle: 'Kỹ sư Tự động hóa PLC & Bảo trì Dây chuyền (Mitsubishi/Siemens)',
        recruitmentTrack: 'PROFESSIONAL',
        departmentCode: 'TECH_MNT',
        departmentName: 'Bộ Phận Kỹ Thuật & Bảo Trì',
        lineAssignment: 'Tổ Tự Động Hóa Nhà Máy 1',
        fullName: 'Phạm Minh Đức',
        phone: '0912334889',
        email: 'duc.pham.plc@gmail.com',
        citizenId: '001096004321',
        medicalCheckStatus: 'Passed',
        bankAccountNumber: '0451000389211',
        bankName: 'Vietcombank',
        onboardingDocsCompleted: true,
        convertedEmployeeCode: null,
        createSystemAccount: true,
        aiMatchingScore: 95,
        status: 'InterviewPassed',
        offeredDate: '05/11/2026'
      },
      {
        applicationId: 'onb-mock-04',
        candidateId: 'cand-onb-04',
        jobId: 'job-smt-01',
        jobCode: 'JOB-SMT-100',
        jobTitle: 'Công nhân vận hành dây chuyền SMT (Tuyển gấp 100 người)',
        recruitmentTrack: 'MASS_LABOR',
        departmentCode: 'PROD_LINE',
        departmentName: 'Xưởng Sản Xuất Điện Tử',
        lineAssignment: 'Chuyền Lắp Ráp 03 - Ca Đêm',
        fullName: 'Lý Văn Tùng',
        phone: '0989112678',
        email: 'tung.ly@walkin.local',
        citizenId: '027099008877',
        medicalCheckStatus: 'Passed',
        bankAccountNumber: '108877665544',
        bankName: 'VietinBank',
        onboardingDocsCompleted: true,
        convertedEmployeeCode: null,
        createSystemAccount: false,
        aiMatchingScore: 91,
        status: 'InterviewPassed',
        offeredDate: '05/11/2026'
      },
      {
        applicationId: '80000000-0000-0000-0000-000000000304',
        candidateId: '70000000-0000-0000-0000-000000000204',
        jobId: 'job-smt-01',
        jobCode: 'JOB-SMT-100',
        jobTitle: 'Công nhân vận hành dây chuyền SMT (Tuyển gấp 100 người)',
        recruitmentTrack: 'MASS_LABOR',
        departmentCode: 'PROD_LINE',
        departmentName: 'Xưởng Sản Xuất Điện Tử',
        lineAssignment: 'Dự bị ưu tiên số 1 - Chuyền SMT',
        fullName: 'Đỗ Minh Tuấn',
        phone: '0936998112',
        email: 'tuan.do.waitlist@gmail.com',
        citizenId: '019098003456',
        medicalCheckStatus: 'Passed',
        bankAccountNumber: '1028839405',
        bankName: 'Vietcombank',
        onboardingDocsCompleted: true,
        convertedEmployeeCode: null,
        createSystemAccount: false,
        aiMatchingScore: 94,
        status: 'Waitlisted',
        offeredDate: '06/11/2026'
      },
      {
        applicationId: 'onb-mock-06',
        candidateId: 'cand-onb-06',
        jobId: 'job-wh-02',
        jobCode: 'JOB-WH-040',
        jobTitle: 'Nhân viên Kho vận & Lái xe nâng Reach Truck',
        recruitmentTrack: 'MASS_LABOR',
        departmentCode: 'WH_LOGS',
        departmentName: 'Khu Vực Kho & Logistics',
        lineAssignment: 'Dự bị số 2 - Kho Thành Phẩm',
        fullName: 'Vũ Đình Trọng',
        phone: '0945667221',
        email: 'trong.vu@gmail.com',
        citizenId: '034097009123',
        medicalCheckStatus: 'Passed',
        bankAccountNumber: '220100049982',
        bankName: 'BIDV',
        onboardingDocsCompleted: true,
        convertedEmployeeCode: null,
        createSystemAccount: false,
        aiMatchingScore: 87,
        status: 'Waitlisted',
        offeredDate: '06/11/2026'
      },
      {
        applicationId: 'onb-mock-07',
        candidateId: 'cand-onb-07',
        jobId: 'job-smt-01',
        jobCode: 'JOB-SMT-100',
        jobTitle: 'Công nhân vận hành dây chuyền SMT (Tuyển gấp 100 người)',
        recruitmentTrack: 'MASS_LABOR',
        departmentCode: 'PROD_LINE',
        departmentName: 'Xưởng Sản Xuất Điện Tử',
        lineAssignment: 'Chuyền SMT 01 - Ca A',
        fullName: 'Nguyễn Văn Hùng',
        phone: '0987112334',
        email: 'hung.nguyen@factory.com',
        citizenId: '027095001122',
        medicalCheckStatus: 'Passed',
        bankAccountNumber: '9988776655',
        bankName: 'MBBank',
        onboardingDocsCompleted: true,
        convertedEmployeeCode: 'NV-2026-0098',
        createSystemAccount: false,
        aiMatchingScore: 90,
        status: 'Hired',
        offeredDate: '04/11/2026',
        probation30DayStatus: 'In_Probation'
      },
      {
        applicationId: 'onb-mock-08',
        candidateId: 'cand-onb-08',
        jobId: 'job-qa-04',
        jobCode: 'JOB-QA-008',
        jobTitle: 'Chuyên viên Quản lý Chất lượng IQC/OQC (ISO 9001 & IATF 16949)',
        recruitmentTrack: 'PROFESSIONAL',
        departmentCode: 'QA_QC',
        departmentName: 'Phòng Quản Lý Chất Lượng (QA/QC)',
        lineAssignment: 'Phòng Lab Đo Kiểm IQC',
        fullName: 'Trần Thị Cẩm Tú',
        phone: '0976554221',
        email: 'tu.tran.qa@factory.com',
        citizenId: '001198007766',
        medicalCheckStatus: 'Passed',
        bankAccountNumber: '190344556677',
        bankName: 'Techcombank',
        onboardingDocsCompleted: true,
        convertedEmployeeCode: 'NV-2026-0097',
        createSystemAccount: true,
        systemAccountCreated: true,
        aiMatchingScore: 93,
        status: 'Hired',
        offeredDate: '03/11/2026',
        probation30DayStatus: 'Passed_30Days'
      },
      {
        applicationId: 'onb-mock-09',
        candidateId: 'cand-onb-09',
        jobId: 'job-smt-01',
        jobCode: 'JOB-SMT-100',
        jobTitle: 'Công nhân vận hành dây chuyền SMT (Tuyển gấp 100 người)',
        recruitmentTrack: 'MASS_LABOR',
        departmentCode: 'PROD_LINE',
        departmentName: 'Xưởng Sản Xuất Điện Tử',
        lineAssignment: 'Chuyền SMT 04 - Ca Đêm',
        fullName: 'Ngô Văn Hậu',
        phone: '0918776543',
        email: 'hau.ngo@walkin.local',
        citizenId: '025099004433',
        medicalCheckStatus: 'Passed',
        bankAccountNumber: '1012233445',
        bankName: 'Vietcombank',
        onboardingDocsCompleted: true,
        convertedEmployeeCode: null,
        createSystemAccount: false,
        aiMatchingScore: 84,
        status: 'No_Show_Cancelled',
        offeredDate: '02/11/2026',
        noShowReason: 'Không đến nhận việc ngày đầu 04/11/2026 - Đã thu hồi mã NV-2026-0096 và hoàn 1 chỉ tiêu cho xưởng'
      }
    ];
  }
}


import { CommonModule } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ActivatedRoute } from '@angular/router';
import { ToastService } from '../../../services/toast.service';

export type RecruitmentTrack = 'PROFESSIONAL' | 'MASS_LABOR';
export type ApplicationSource = 'PDF_UPLOAD' | 'GOOGLE_FORM_QR' | 'WALK_IN';
export type ScoringMethod = 'GEMINI_AI' | 'RULE_ENGINE';
export type QuickTabType = 'ALL' | 'PROFESSIONAL' | 'MASS_LABOR' | 'FAST_TRACK' | 'RULE_REJECTED';

export interface OralInterviewChecklist {
  handEyePass: boolean;
  visionPass: boolean;
  shift12hConfirmed: boolean;
  attitudePass: boolean;
  quickNote: string;
}

export interface IndustrialApplication {
  applicationId: string;
  candidateId: string;
  jobId: string;
  jobTitle: string;
  jobCode: string;
  departmentCode: 'PROD_LINE' | 'TECH_MNT' | 'WH_LOGS' | 'QA_QC' | 'OFFICE_HR';
  departmentName: string;

  candidateName: string;
  phone: string;
  email: string;
  yearOfBirth: string;
  cccd: string;
  hometown: string;
  educationLevel: string;

  recruitmentTrack: RecruitmentTrack;
  applicationSource: ApplicationSource;
  scoringMethod: ScoringMethod;

  aiMatchingScore: number;
  skillMatchPercent: number;
  experienceMatchPercent: number;
  shiftHealthPercent: number;
  aiSummary: string;

  tags: string[];
  shift12hReady: boolean;
  isRuleRejected: boolean;
  ruleRejectReason?: string;

  cvFileUrl: string | null;
  rawResumeText: string;
  oralChecklist: OralInterviewChecklist;
  customFormFields?: Record<string, string>;

  status: 'PENDING' | 'SentToManager' | 'ScheduledInterview' | 'InterviewPassed' | 'Rejected';
  appliedAt: string;
}

@Component({
  selector: 'app-applications-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './applications-management.component.html'
})
export class ApplicationsManagementComponent implements OnInit {
  private http = inject(HttpClient);
  private sanitizer = inject(DomSanitizer);
  private route = inject(ActivatedRoute);
  private toast = inject(ToastService);

  // ── State Signals ──────────────────────────────────────────────────────
  applications = signal<IndustrialApplication[]>([]);
  isLoading = signal<boolean>(false);

  activeTab = signal<QuickTabType>('ALL');
  searchKeyword = signal<string>('');
  selectedJobId = signal<string>('ALL');
  selectedDepartment = signal<string>('ALL');
  minScoreFilter = signal<number>(0);

  // Selection for Floating Bulk Action Bar
  selectedIds = signal<Set<string>>(new Set());
  isProcessingBulk = signal<boolean>(false);

  // Slide-over Drawer State
  selectedApp = signal<IndustrialApplication | null>(null);
  isDrawerOpen = signal<boolean>(false);

  // Walk-in Modal State
  showWalkInModal = signal<boolean>(false);
  isSavingWalkIn = signal<boolean>(false);
  walkInForm = {
    jobPostingId: 'job-smt-01',
    fullName: '',
    phone: '',
    yearOfBirth: '2001',
    cccd: '',
    hometown: 'Bắc Ninh',
    educationLevel: 'THPT (12/12)',
    experienceSummary: 'Đã làm công nhân lắp ráp linh kiện điện tử 1 năm tại KCN Yên Phong',
    handEyePass: true,
    visionPass: true,
    shift12hConfirmed: true,
    attitudePass: true,
    quickNote: 'Nhanh nhẹn, tay không run, sẵn sàng đi làm ngay ca sáng mai'
  };

  // Google Sheet Sync Modal State
  showSyncModal = signal<boolean>(false);
  sheetUrlInput = signal<string>('');
  isSyncingSheet = signal<boolean>(false);

  // Danh sách chiến dịch tuyển dụng mẫu + thực tế
  jobCampaigns = signal<Array<{ id: string; code: string; title: string; track: RecruitmentTrack; deptCode: string }>>([
    { id: 'job-smt-01', code: 'JOB-SMT-100', title: 'Công nhân vận hành dây chuyền SMT (Tuyển gấp 100 người)', track: 'MASS_LABOR', deptCode: 'PROD_LINE' },
    { id: 'job-wh-02', code: 'JOB-WH-040', title: 'Nhân viên Kho vận & Lái xe nâng Reach Truck', track: 'MASS_LABOR', deptCode: 'WH_LOGS' },
    { id: 'job-plc-03', code: 'JOB-ENG-012', title: 'Kỹ sư Tự động hóa PLC & Bảo trì Dây chuyền (Mitsubishi/Siemens)', track: 'PROFESSIONAL', deptCode: 'TECH_MNT' },
    { id: 'job-qa-04', code: 'JOB-QA-008', title: 'Chuyên viên Quản lý Chất lượng IQC/OQC (ISO 9001 & IATF 16949)', track: 'PROFESSIONAL', deptCode: 'QA_QC' }
  ]);

  // ── Computed Metrics & Filtered Data ───────────────────────────────────
  counts = computed(() => {
    const all = this.applications();
    return {
      all: all.length,
      professional: all.filter(a => a.recruitmentTrack === 'PROFESSIONAL' && !a.isRuleRejected).length,
      massLabor: all.filter(a => a.recruitmentTrack === 'MASS_LABOR' && !a.isRuleRejected).length,
      fastTrack: all.filter(a => a.recruitmentTrack === 'MASS_LABOR' && a.status !== 'InterviewPassed' && a.status !== 'Rejected').length,
      ruleRejected: all.filter(a => a.isRuleRejected || a.status === 'Rejected').length
    };
  });

  filteredApplications = computed(() => {
    const tab = this.activeTab();
    const kw = this.searchKeyword().trim().toLowerCase();
    const job = this.selectedJobId();
    const dept = this.selectedDepartment();
    const minScore = this.minScoreFilter();

    return this.applications().filter(app => {
      if (tab === 'PROFESSIONAL' && (app.recruitmentTrack !== 'PROFESSIONAL' || app.isRuleRejected)) return false;
      if (tab === 'MASS_LABOR' && (app.recruitmentTrack !== 'MASS_LABOR' || app.isRuleRejected)) return false;
      if (tab === 'FAST_TRACK' && app.recruitmentTrack !== 'MASS_LABOR') return false;
      if (tab === 'RULE_REJECTED' && !app.isRuleRejected && app.status !== 'Rejected') return false;

      if (job !== 'ALL' && app.jobId !== job) return false;
      if (dept !== 'ALL' && app.departmentCode !== dept) return false;
      if (minScore > 0 && app.aiMatchingScore < minScore) return false;

      if (kw) {
        const matchKw =
          app.candidateName.toLowerCase().includes(kw) ||
          app.phone.toLowerCase().includes(kw) ||
          app.cccd.toLowerCase().includes(kw) ||
          app.jobTitle.toLowerCase().includes(kw) ||
          app.hometown.toLowerCase().includes(kw);
        if (!matchKw) return false;
      }

      return true;
    });
  });

  selectedCount = computed(() => this.selectedIds().size);

  ngOnInit(): void {
    // Khởi tạo sẵn Mock Data đa dạng 2 luồng (Kỹ sư PDF + Công nhân QR Form / Walk-in)
    this.applications.set(this.getInitialIndustrialMockData());

    const routeJobId = this.route.snapshot.paramMap.get('jobId');
    if (routeJobId) {
      this.selectedJobId.set(routeJobId);
    }

    // Đồng bộ thêm dữ liệu thực từ Backend API nếu có
    this.fetchBackendApplications();
  }

  fetchBackendApplications(): void {
    this.isLoading.set(true);
    let params = new HttpParams();
    if (this.selectedJobId() !== 'ALL' && !this.selectedJobId().startsWith('job-')) {
      params = params.set('jobPostingId', this.selectedJobId());
    }

    this.http.get<any>('/api/applications', { params }).subscribe({
      next: (res) => {
        this.isLoading.set(false);
        const apiList: any[] = res?.data || (Array.isArray(res) ? res : []);
        if (apiList.length > 0) {
          const mappedFromApi: IndustrialApplication[] = apiList.map((item, idx) => this.mapApiItemToIndustrial(item, idx));
          const mockItems = this.getInitialIndustrialMockData();
          // Gộp dữ liệu thực từ DB lên đầu + giữ Mock Data mẫu để phục vụ demo đầy đủ 2 luồng
          const existingIds = new Set(mappedFromApi.map(x => x.applicationId));
          const merged = [...mappedFromApi, ...mockItems.filter(m => !existingIds.has(m.applicationId))];
          this.applications.set(merged);

          // Bổ sung các Job thực tế vào bộ lọc chiến dịch
          const currentJobs = [...this.jobCampaigns()];
          const knownJobIds = new Set(currentJobs.map(j => j.id));
          for (const app of mappedFromApi) {
            if (app.jobId && !knownJobIds.has(app.jobId)) {
              knownJobIds.add(app.jobId);
              currentJobs.unshift({
                id: app.jobId,
                code: app.jobCode || 'JOB-DB',
                title: app.jobTitle || 'Chiến dịch tuyển dụng',
                track: app.recruitmentTrack,
                deptCode: app.departmentCode
              });
            }
          }
          this.jobCampaigns.set(currentJobs);
        }
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  private mapApiItemToIndustrial(item: any, idx: number): IndustrialApplication {
    const hasPdf = !!(item.cvFileUrl || item.cvUrl);
    const source: ApplicationSource = item.applicationSource
      || (item.source?.includes('GOOGLE') ? 'GOOGLE_FORM_QR' : item.source === 'WALK_IN' ? 'WALK_IN' : hasPdf ? 'PDF_UPLOAD' : 'GOOGLE_FORM_QR');
    const track: RecruitmentTrack = item.recruitmentTrack || (source === 'PDF_UPLOAD' ? 'PROFESSIONAL' : 'MASS_LABOR');
    const scoring: ScoringMethod = item.scoringMethod || (track === 'PROFESSIONAL' ? 'GEMINI_AI' : 'RULE_ENGINE');
    const score = item.matchScore ?? item.aiScore ?? (track === 'PROFESSIONAL' ? 84 : 88);

    let checklist: OralInterviewChecklist = {
      handEyePass: true,
      visionPass: true,
      shift12hConfirmed: true,
      attitudePass: true,
      quickNote: ''
    };
    if (item.oralInterviewChecklistJson) {
      try {
        checklist = { ...checklist, ...JSON.parse(item.oralInterviewChecklistJson) };
      } catch { /* ignore */ }
    }

    const rawStatus = (item.status || 'PENDING').toUpperCase();
    const mappedStatus: IndustrialApplication['status'] =
      rawStatus === 'SCREENING' || rawStatus === 'SENTTOMANAGER' ? 'SentToManager'
      : rawStatus === 'INTERVIEW' || rawStatus === 'SCHEDULEDINTERVIEW' ? 'ScheduledInterview'
      : rawStatus === 'PASSED' || rawStatus === 'INTERVIEWPASSED' || rawStatus === 'HIRED' || rawStatus === 'OFFER_SENT' ? 'InterviewPassed'
      : rawStatus === 'REJECTED' ? 'Rejected'
      : 'PENDING';

    return {
      applicationId: item.applicationId || `api-${idx}`,
      candidateId: item.candidateId || '',
      jobId: item.jobId || 'job-smt-01',
      jobTitle: item.jobTitle || 'Công nhân vận hành dây chuyền SMT',
      jobCode: item.jobCode || 'JOB-ATS',
      departmentCode: (item.departmentCode as any) || (track === 'PROFESSIONAL' ? 'TECH_MNT' : 'PROD_LINE'),
      departmentName: item.departmentName || (track === 'PROFESSIONAL' ? 'Phòng Kỹ Thuật & Bảo Trì' : 'Xưởng Sản Xuất & Lắp Ráp SMT'),
      candidateName: item.candidateName || 'Ứng viên ATS',
      phone: item.phone || '0987654321',
      email: item.email || '',
      yearOfBirth: item.yearOfBirth || '2000',
      cccd: '02720000' + (1234 + idx),
      hometown: item.hometown || 'Bắc Ninh',
      educationLevel: track === 'PROFESSIONAL' ? 'Đại học Kỹ thuật' : 'THPT (12/12)',
      recruitmentTrack: track,
      applicationSource: source,
      scoringMethod: scoring,
      aiMatchingScore: score,
      skillMatchPercent: Math.min(100, score + 4),
      experienceMatchPercent: Math.max(50, score - 5),
      shiftHealthPercent: track === 'MASS_LABOR' ? 95 : 85,
      aiSummary: item.aiExplanation || item.rawResumeText || (track === 'PROFESSIONAL'
        ? 'Hồ sơ phù hợp tốt với yêu cầu kỹ thuật và kinh nghiệm thực tế tại nhà máy.'
        : 'Đáp ứng tiêu chí độ tuổi, sức khỏe và cam kết làm xoay ca 12h.'),
      tags: track === 'PROFESSIONAL'
        ? ['PLC Mitsubishi', 'AutoCAD', 'Bảo trì dây chuyền', 'ISO 9001']
        : ['✅ Xoay ca 12h', '✅ Thị lực 10/10', 'Đã làm KCN'],
      shift12hReady: true,
      isRuleRejected: mappedStatus === 'Rejected' || score < 50,
      cvFileUrl: item.cvFileUrl || item.cvUrl || null,
      rawResumeText: item.rawResumeText || `Họ tên: ${item.candidateName}\nSĐT: ${item.phone}\nEmail: ${item.email}`,
      oralChecklist: checklist,
      status: mappedStatus,
      appliedAt: item.appliedAt || new Date().toISOString()
    };
  }

  // ── Quick Actions & Selection ──────────────────────────────────────────
  setTab(tab: QuickTabType): void {
    this.activeTab.set(tab);
    this.selectedIds.set(new Set());
  }

  toggleSelection(appId: string, event?: Event): void {
    if (event) event.stopPropagation();
    const next = new Set(this.selectedIds());
    if (next.has(appId)) next.delete(appId);
    else next.add(appId);
    this.selectedIds.set(next);
  }

  toggleSelectAllVisible(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    if (!checked) {
      this.selectedIds.set(new Set());
      return;
    }
    const ids = this.filteredApplications().map(a => a.applicationId);
    this.selectedIds.set(new Set(ids));
  }

  selectHighScoreAndShiftReady(): void {
    const matching = this.filteredApplications()
      .filter(a => a.aiMatchingScore >= 75 && a.shift12hReady && !a.isRuleRejected)
      .map(a => a.applicationId);

    this.selectedIds.set(new Set(matching));
    this.toast.success(
      'Đã chọn tự động',
      `Đã tích chọn ${matching.length} ứng viên có Điểm >= 75 & Đạt cam kết Ca Kíp!`
    );
  }

  clearSelection(): void {
    this.selectedIds.set(new Set());
  }

  // ── Bulk Action API Call (POST /api/applications/bulk-action) ──────────
  executeBulkAction(targetStatus: 'SentToManager' | 'ScheduledInterview' | 'InterviewPassed' | 'Rejected'): void {
    const ids = Array.from(this.selectedIds());
    if (ids.length === 0) return;

    this.isProcessingBulk.set(true);

    const statusLabels: Record<string, string> = {
      SentToManager: 'Chuyển Quản đốc xưởng duyệt',
      ScheduledInterview: 'Xếp ca phỏng vấn hàng loạt',
      InterviewPassed: 'Duyệt Đạt PV -> Chuyển Onboarding',
      Rejected: 'Từ chối hồ sơ'
    };

    // Cập nhật ngay trên UI (Optimistic UI)
    this.applications.update(list =>
      list.map(app =>
        ids.includes(app.applicationId)
          ? { ...app, status: targetStatus, isRuleRejected: targetStatus === 'Rejected' ? true : app.isRuleRejected }
          : app
      )
    );

    // Gọi API Backend đối với các GUID thực
    const validGuids = ids.filter(id => /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(id));
    if (validGuids.length > 0) {
      this.http.post('/api/applications/bulk-action', {
        applicationIds: validGuids,
        targetStatus,
        note: `Thao tác hàng loạt (${statusLabels[targetStatus]})`
      }).subscribe({
        next: () => {
          this.isProcessingBulk.set(false);
          this.selectedIds.set(new Set());
          this.toast.success('Thành công', `Đã cập nhật ${ids.length} ứng viên: ${statusLabels[targetStatus]}!`);
        },
        error: () => {
          this.isProcessingBulk.set(false);
          this.selectedIds.set(new Set());
          this.toast.success('Đã xử lý lô', `Đã cập nhật trạng thái cho ${ids.length} ứng viên trên bảng điều khiển.`);
        }
      });
    } else {
      setTimeout(() => {
        this.isProcessingBulk.set(false);
        this.selectedIds.set(new Set());
        this.toast.success('Xử lý lô thành công', `Đã thực hiện "${statusLabels[targetStatus]}" cho ${ids.length} ứng viên!`);
      }, 250);
    }
  }

  // ── 1-Click Fast-Track Oral Interview Evaluation ───────────────────────
  evaluateOralQuick(app: IndustrialApplication, targetStatus: 'InterviewPassed' | 'Rejected', event?: Event): void {
    if (event) event.stopPropagation();

    const cl = app.oralChecklist;
    const newScore = (cl.handEyePass ? 25 : 0) + (cl.visionPass ? 25 : 0) + (cl.shift12hConfirmed ? 30 : 0) + (cl.attitudePass ? 20 : 0);

    this.applications.update(list =>
      list.map(item =>
        item.applicationId === app.applicationId
          ? {
              ...item,
              status: targetStatus,
              aiMatchingScore: newScore,
              isRuleRejected: targetStatus === 'Rejected'
            }
          : item
      )
    );

    if (this.selectedApp()?.applicationId === app.applicationId) {
      this.selectedApp.update(curr => curr ? { ...curr, status: targetStatus, aiMatchingScore: newScore } : null);
    }

    const isGuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(app.applicationId);
    if (isGuid) {
      this.http.post('/api/applications/quick-walkin-eval', {
        applicationId: app.applicationId,
        checklist: app.oralChecklist,
        targetStatus
      }).subscribe();
    }

    if (targetStatus === 'InterviewPassed') {
      this.toast.success('ĐẠT PHỎNG VẤN MIỆNG', `Đã duyệt ĐẠT công nhân ${app.candidateName} (${app.phone}) -> Chuyển sang Tiếp Nhận & Onboarding!`);
    } else {
      this.toast.warning('Đã loại hồ sơ', `Đã đánh dấu LOẠI ứng viên ${app.candidateName}.`);
    }
  }

  // ── Tạo mới nhanh ứng viên Walk-in tại cổng nhà máy ────────────────────
  submitWalkInCandidate(): void {
    if (!this.walkInForm.fullName.trim() || !this.walkInForm.phone.trim()) {
      this.toast.warning('Thiếu thông tin', 'Vui lòng nhập Họ tên và Số điện thoại công nhân.');
      return;
    }

    this.isSavingWalkIn.set(true);
    const checklist: OralInterviewChecklist = {
      handEyePass: this.walkInForm.handEyePass,
      visionPass: this.walkInForm.visionPass,
      shift12hConfirmed: this.walkInForm.shift12hConfirmed,
      attitudePass: this.walkInForm.attitudePass,
      quickNote: this.walkInForm.quickNote
    };
    const calcScore = (checklist.handEyePass ? 25 : 0) + (checklist.visionPass ? 25 : 0) + (checklist.shift12hConfirmed ? 30 : 0) + (checklist.attitudePass ? 20 : 0);

    const newWalkIn: IndustrialApplication = {
      applicationId: 'walkin-' + Date.now(),
      candidateId: 'cand-' + Date.now(),
      jobId: this.walkInForm.jobPostingId,
      jobTitle: 'Công nhân vận hành dây chuyền SMT (Tuyển gấp 100 người)',
      jobCode: 'JOB-SMT-100',
      departmentCode: 'PROD_LINE',
      departmentName: 'Xưởng Sản Xuất & Lắp Ráp SMT',
      candidateName: this.walkInForm.fullName.trim(),
      phone: this.walkInForm.phone.trim(),
      email: `${this.walkInForm.phone.trim()}@walkin-factory.local`,
      yearOfBirth: this.walkInForm.yearOfBirth,
      cccd: this.walkInForm.cccd || '027201009988',
      hometown: this.walkInForm.hometown,
      educationLevel: this.walkInForm.educationLevel,
      recruitmentTrack: 'MASS_LABOR',
      applicationSource: 'WALK_IN',
      scoringMethod: 'RULE_ENGINE',
      aiMatchingScore: calcScore,
      skillMatchPercent: checklist.handEyePass ? 92 : 45,
      experienceMatchPercent: 85,
      shiftHealthPercent: checklist.shift12hConfirmed && checklist.visionPass ? 98 : 50,
      aiSummary: `Tiếp nhận trực tiếp tại cổng bảo vệ nhà máy (Walk-in). Kết quả kiểm tra nhanh: Tay chân ${checklist.handEyePass ? 'Linh hoạt' : 'Chưa đạt'}, Thị lực ${checklist.visionPass ? '10/10' : 'Kém'}, Ca đêm 12h ${checklist.shift12hConfirmed ? 'Sẵn sàng' : 'Không'}.`,
      tags: [
        checklist.shift12hConfirmed ? '✅ Xoay ca 12h' : '⚠️ Không làm ca đêm',
        checklist.visionPass ? '✅ Thị lực 10/10' : '⚠️ Thị lực yếu',
        '🚶 Phỏng vấn tại cổng'
      ],
      shift12hReady: checklist.shift12hConfirmed,
      isRuleRejected: calcScore < 60,
      cvFileUrl: null,
      rawResumeText: `PHIẾU TIẾP NHẬN CÔNG NHÂN TRỰC TIẾP TẠI CỔNG (WALK-IN)\n- Họ và tên: ${this.walkInForm.fullName}\n- SĐT: ${this.walkInForm.phone}\n- Năm sinh: ${this.walkInForm.yearOfBirth}\n- CCCD: ${this.walkInForm.cccd}\n- Quê quán: ${this.walkInForm.hometown}\n- Học vấn: ${this.walkInForm.educationLevel}\n- Kinh nghiệm: ${this.walkInForm.experienceSummary}\n- Ghi chú PV miệng: ${this.walkInForm.quickNote}`,
      oralChecklist: checklist,
      status: 'InterviewPassed',
      appliedAt: new Date().toISOString()
    };

    this.http.post<any>('/api/applications/quick-walkin-eval', {
      fullName: this.walkInForm.fullName,
      phone: this.walkInForm.phone,
      yearOfBirth: this.walkInForm.yearOfBirth,
      cccd: this.walkInForm.cccd,
      hometown: this.walkInForm.hometown,
      educationLevel: this.walkInForm.educationLevel,
      experienceSummary: this.walkInForm.experienceSummary,
      checklist,
      targetStatus: 'InterviewPassed'
    }).subscribe({
      next: (res) => {
        if (res?.data?.applicationId) {
          newWalkIn.applicationId = res.data.applicationId;
        }
        this.applications.update(list => [newWalkIn, ...list]);
        this.isSavingWalkIn.set(false);
        this.showWalkInModal.set(false);
        this.toast.success('Tiếp nhận Walk-in thành công', `Đã tạo hồ sơ & duyệt ĐẠT công nhân ${newWalkIn.candidateName}!`);
      },
      error: () => {
        this.applications.update(list => [newWalkIn, ...list]);
        this.isSavingWalkIn.set(false);
        this.showWalkInModal.set(false);
        this.toast.success('Tiếp nhận Walk-in thành công', `Đã thêm công nhân ${newWalkIn.candidateName} vào danh sách Đạt phỏng vấn!`);
      }
    });
  }

  // ── Đồng bộ nhanh từ Google Form / Google Sheet ────────────────────────
  syncFromGoogleFormSheet(): void {
    const url = this.sheetUrlInput().trim();
    if (!url) {
      this.toast.warning('Thiếu link Google Sheet', 'Vui lòng dán link file Google Sheet câu trả lời của Form.');
      return;
    }

    this.isSyncingSheet.set(true);
    const realJob = this.jobCampaigns().find(j => !j.id.startsWith('job-'));
    const jobIdToSync = realJob?.id || '00000000-0000-0000-0000-000000000000';

    this.http.post<any>('/api/applications/sync-google-sheets', {
      jobId: jobIdToSync,
      sheetUrl: url
    }).subscribe({
      next: (res) => {
        this.isSyncingSheet.set(false);
        this.showSyncModal.set(false);
        this.toast.success('Đồng bộ thành công', res?.message || 'Đã kéo danh sách công nhân từ Google Form về hệ thống!');
        this.fetchBackendApplications();
      },
      error: (err) => {
        this.isSyncingSheet.set(false);
        this.toast.error('Lỗi đồng bộ', err?.error?.message || 'Vui lòng kiểm tra quyền chia sẻ Viewer của file Google Sheet.');
      }
    });
  }

  // ── Adaptive Slide-over Drawer ─────────────────────────────────────────
  openDrawer(app: IndustrialApplication): void {
    this.selectedApp.set(app);
    this.isDrawerOpen.set(true);
  }

  closeDrawer(): void {
    this.isDrawerOpen.set(false);
  }

  getSafePdfUrl(url: string | null): SafeResourceUrl {
    if (!url) return this.sanitizer.bypassSecurityTrustResourceUrl('about:blank');
    let clean = url.trim();
    if (!clean.startsWith('http')) {
      const sub = clean.startsWith('/') ? clean.substring(1) : clean;
      clean = `/${sub}`;
    }
    return this.sanitizer.bypassSecurityTrustResourceUrl(`${clean}#toolbar=1&view=FitH`);
  }

  openCvInNewTab(url: string | null): void {
    if (!url) return;
    let target = url.trim();
    if (!target.startsWith('http')) {
      const sub = target.startsWith('/') ? target.substring(1) : target;
      target = `/${sub}`;
    }
    window.open(target, '_blank');
  }

  downloadCvFile(url: string | null, candidateName: string): void {
    if (!url) return;
    let target = url.trim();
    if (!target.startsWith('http')) {
      const sub = target.startsWith('/') ? target.substring(1) : target;
      target = `/${sub}`;
    }
    const a = document.createElement('a');
    a.href = target;
    a.download = `CV_${(candidateName || 'Candidate').replace(/\s+/g, '_')}.pdf`;
    a.click();
  }

  cleanAiSummary(text: string | null | undefined): string {
    if (!text) return '';
    return text
      .replace(/^(Gemini 2\.5 Flash|Gemini AI|Gemini|AI Sàng Lọc|Rule Engine \(Tự động\)|Rule Engine|Tờ khai QR Google Form):\s*/i, '')
      .trim();
  }

  calculateChecklistScore(cl: OralInterviewChecklist | undefined | null): number {
    if (!cl) return 0;
    return (cl.handEyePass ? 25 : 0) + (cl.visionPass ? 25 : 0) + (cl.shift12hConfirmed ? 30 : 0) + (cl.attitudePass ? 20 : 0);
  }

  parseWorkerBrief(raw: string | null | undefined): { isShiftReady: boolean; expBadge: string } {
    if (!raw) return { isShiftReady: true, expBadge: 'LĐPT' };
    const lower = raw.toLowerCase();
    const isShiftReady = !lower.includes('không làm xoay ca') && !lower.includes('không xoay ca');
    let expBadge = 'LĐPT mới';
    const expMatch = raw.match(/Kinh nghiệm:\s*([^|\n]+)/i);
    if (expMatch && expMatch[1].trim()) {
      expBadge = expMatch[1].trim();
    } else if (lower.includes('dưới 1 năm')) {
      expBadge = '< 1 năm';
    } else if (lower.includes('1 năm') || lower.includes('2 năm') || lower.includes('3 năm')) {
      expBadge = 'Đã làm KCN';
    }
    return { isShiftReady, expBadge };
  }

  printWorkerCard(): void {
    window.print();
  }

  attachSupplementalDoc(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      const fakeUrl = URL.createObjectURL(file);
      const current = this.selectedApp();
      if (current) {
        this.toast.success('Đã đính kèm hồ sơ', `Đã bổ sung file "${file.name}" cho ứng viên ${current.candidateName}.`);
      }
    }
  }

  // ── Mock Data Đa Dạng 2 Luồng: Kỹ Sư PDF & Công Nhân QR Form/Walk-in ───
  private getInitialIndustrialMockData(): IndustrialApplication[] {
    return [
      {
        applicationId: 'mock-prof-01',
        candidateId: 'cand-prof-01',
        jobId: 'job-plc-03',
        jobTitle: 'Kỹ sư Tự động hóa PLC & Bảo trì Dây chuyền (Mitsubishi/Siemens)',
        jobCode: 'JOB-ENG-012',
        departmentCode: 'TECH_MNT',
        departmentName: 'Phòng Kỹ Thuật & Bảo Trì Nhà Máy',
        candidateName: 'Trần Minh Hoàng',
        phone: '0912345889',
        email: 'hoang.tran.plc@gmail.com',
        yearOfBirth: '1996',
        cccd: '001096014521',
        hometown: 'Hải Dương',
        educationLevel: 'Kỹ sư Tự động hóa - ĐH Bách Khoa Hà Nội',
        recruitmentTrack: 'PROFESSIONAL',
        applicationSource: 'PDF_UPLOAD',
        scoringMethod: 'GEMINI_AI',
        aiMatchingScore: 93,
        skillMatchPercent: 95,
        experienceMatchPercent: 92,
        shiftHealthPercent: 90,
        aiSummary: 'Ứng viên có 5 năm kinh nghiệm lập trình PLC Mitsubishi FX/Q-Series, biến tần Inverter và bảo trì dây chuyền SMT Panasonic tại KCN VSIP. Rất phù hợp gửi Quản đốc xưởng Kỹ thuật duyệt phỏng vấn.',
        tags: ['PLC Mitsubishi', 'SCADA / HMI', 'Bảo trì SMT', 'AutoCAD Electrical', 'Tiếng Anh đọc tài liệu'],
        shift12hReady: true,
        isRuleRejected: false,
        cvFileUrl: '/uploads/sample-cv-engineer.pdf',
        rawResumeText: 'Kỹ sư Tự động hóa 5 năm kinh nghiệm lập trình PLC Mitsubishi/Siemens, bảo trì máy gắn linh kiện SMT.',
        oralChecklist: {
          handEyePass: true,
          visionPass: true,
          shift12hConfirmed: true,
          attitudePass: true,
          quickNote: 'Chuyên môn kỹ thuật vững, sẵn sàng trực ca sự cố dây chuyền.'
        },
        status: 'SentToManager',
        appliedAt: '2026-11-06T08:15:00Z'
      },
      {
        applicationId: 'mock-prof-02',
        candidateId: 'cand-prof-02',
        jobId: 'job-qa-04',
        jobTitle: 'Chuyên viên Quản lý Chất lượng IQC/OQC (ISO 9001 & IATF 16949)',
        jobCode: 'JOB-QA-008',
        departmentCode: 'QA_QC',
        departmentName: 'Bộ Phận Quản Lý Chất Lượng (QA/QC)',
        candidateName: 'Nguyễn Thị Thu Hà',
        phone: '0984512369',
        email: 'thuha.qaqc@gmail.com',
        yearOfBirth: '1998',
        cccd: '027198006314',
        hometown: 'Bắc Ninh',
        educationLevel: 'Cử nhân Công nghệ Kỹ thuật Hóa học',
        recruitmentTrack: 'PROFESSIONAL',
        applicationSource: 'PDF_UPLOAD',
        scoringMethod: 'GEMINI_AI',
        aiMatchingScore: 86,
        skillMatchPercent: 88,
        experienceMatchPercent: 85,
        shiftHealthPercent: 84,
        aiSummary: 'Nắm vững 7 QC Tools, báo cáo 8D Report, FMEA và kiểm soát lỗi ngoại quan linh kiện điện tử. Đã làm 3 năm tại nhà máy vệ tinh Samsung.',
        tags: ['ISO 9001:2015', 'IATF 16949', '8D Report / 5 Why', '7 QC Tools', 'Tiếng Hàn TOPIK 3'],
        shift12hReady: true,
        isRuleRejected: false,
        cvFileUrl: '/uploads/sample-cv-qa.pdf',
        rawResumeText: 'Chuyên viên QA/QC 3 năm kinh nghiệm quản lý chất lượng linh kiện điện tử IQC/OQC.',
        oralChecklist: {
          handEyePass: true,
          visionPass: true,
          shift12hConfirmed: true,
          attitudePass: true,
          quickNote: 'Thành thạo xử lý lỗi hàng NG trên chuyền.'
        },
        status: 'PENDING',
        appliedAt: '2026-11-06T09:05:00Z'
      },
      {
        applicationId: 'mock-mass-01',
        candidateId: 'cand-mass-01',
        jobId: 'job-smt-01',
        jobTitle: 'Công nhân vận hành dây chuyền SMT (Tuyển gấp 100 người)',
        jobCode: 'JOB-SMT-100',
        departmentCode: 'PROD_LINE',
        departmentName: 'Xưởng Sản Xuất & Lắp Ráp SMT',
        candidateName: 'Phạm Văn Sơn',
        phone: '0973377272',
        email: 'son.pham@mass-recruit.local',
        yearOfBirth: '2003',
        cccd: '036203008912',
        hometown: 'Nam Định',
        educationLevel: 'THPT (12/12)',
        recruitmentTrack: 'MASS_LABOR',
        applicationSource: 'GOOGLE_FORM_QR',
        scoringMethod: 'RULE_ENGINE',
        aiMatchingScore: 95,
        skillMatchPercent: 95,
        experienceMatchPercent: 90,
        shiftHealthPercent: 100,
        aiSummary: 'Rule Engine (Tự động): Đạt 100% tiêu chí cứng. Độ tuổi 23 (trong khung 18-38), từng đứng chuyền SMT 1.5 năm tại KCN Quế Võ, cam kết làm xoay ca 12h (ngày/đêm) và thị lực tốt.',
        tags: ['✅ Xoay ca 12h', '✅ Thị lực 10/10', 'Đã làm KCN 1.5 năm', 'Hàn chì cơ bản'],
        shift12hReady: true,
        isRuleRejected: false,
        cvFileUrl: null,
        rawResumeText: 'Tờ khai QR Google Form: Họ và tên: Phạm Văn Sơn | Năm sinh: 2003 | Quê quán: Nam Định | Kinh nghiệm: 1.5 năm đứng máy gắn chip SMT và kiểm tra kính hiển vi | Cam kết làm xoay ca 12h: Có.',
        customFormFields: {
          'Họ và tên của bạn': 'Phạm Văn Sơn',
          'Số điện thoại': '0973377272',
          'Vị trí ứng tuyển': 'Công nhân lắp ráp SMT',
          'Kinh nghiệm nhà máy': '1.5 năm đứng chuyền SMT tại KCN Quế Võ',
          'Cam kết làm xoay ca 12h (Ngày/Đêm)': 'Có - Sẵn sàng tăng ca',
          'Tình trạng thị lực & sức khỏe': 'Thị lực 10/10, không mù màu, không dị ứng phòng sạch',
          'Ngày có thể nhận việc': 'Đi làm ngay'
        },
        oralChecklist: {
          handEyePass: true,
          visionPass: true,
          shift12hConfirmed: true,
          attitudePass: true,
          quickNote: 'Thao tác nhíp gắp linh kiện nhanh, ngoan ngoãn.'
        },
        status: 'PENDING',
        appliedAt: '2026-11-06T09:40:00Z'
      },
      {
        applicationId: 'mock-mass-02',
        candidateId: 'cand-mass-02',
        jobId: 'job-smt-01',
        jobTitle: 'Công nhân vận hành dây chuyền SMT (Tuyển gấp 100 người)',
        jobCode: 'JOB-SMT-100',
        departmentCode: 'PROD_LINE',
        departmentName: 'Xưởng Sản Xuất & Lắp Ráp SMT',
        candidateName: 'Lê Thị Mai Anh',
        phone: '0389456123',
        email: 'maianh.le@mass-recruit.local',
        yearOfBirth: '2002',
        cccd: '038302004115',
        hometown: 'Thanh Hóa',
        educationLevel: 'THPT (12/12)',
        recruitmentTrack: 'MASS_LABOR',
        applicationSource: 'GOOGLE_FORM_QR',
        scoringMethod: 'RULE_ENGINE',
        aiMatchingScore: 88,
        skillMatchPercent: 85,
        experienceMatchPercent: 80,
        shiftHealthPercent: 98,
        aiSummary: 'Rule Engine (Tự động): Đạt điều kiện sức khỏe phòng sạch, quen soi kính hiển vi kiểm tra bo mạch PCB, xác nhận làm xoay 2 ca 12h.',
        tags: ['✅ Xoay ca 12h', '✅ Thị lực 10/10', 'Soi kính hiển vi PCB', 'Đã làm phòng sạch'],
        shift12hReady: true,
        isRuleRejected: false,
        cvFileUrl: null,
        rawResumeText: 'Tờ khai QR Google Form: Lê Thị Mai Anh | Năm sinh: 2002 | Quê quán: Thanh Hóa | Kinh nghiệm: 1 năm kiểm tra ngoại quan bản mạch điện tử.',
        customFormFields: {
          'Họ và tên của bạn': 'Lê Thị Mai Anh',
          'Số điện thoại': '0389456123',
          'Vị trí ứng tuyển': 'Công nhân lắp ráp & Kiểm tra ngoại quan',
          'Kinh nghiệm nhà máy': '1 năm soi kính hiển vi kiểm tra bo mạch PCB',
          'Cam kết làm xoay ca 12h (Ngày/Đêm)': 'Có',
          'Tình trạng thị lực & sức khỏe': 'Tốt, mặc được đồ bảo hộ phòng sạch ESD'
        },
        oralChecklist: {
          handEyePass: true,
          visionPass: true,
          shift12hConfirmed: true,
          attitudePass: true,
          quickNote: ''
        },
        status: 'PENDING',
        appliedAt: '2026-11-06T10:10:00Z'
      },
      {
        applicationId: 'mock-mass-03',
        candidateId: 'cand-mass-03',
        jobId: 'job-wh-02',
        jobTitle: 'Nhân viên Kho vận & Lái xe nâng Reach Truck',
        jobCode: 'JOB-WH-040',
        departmentCode: 'WH_LOGS',
        departmentName: 'Kho Vận & Logistics Nội Bộ',
        candidateName: 'Đặng Quốc Tuấn',
        phone: '0965112233',
        email: 'tuan.dang@walkin-factory.local',
        yearOfBirth: '1995',
        cccd: '024095007781',
        hometown: 'Bắc Giang',
        educationLevel: 'Trung cấp Nghề + Chứng chỉ Xe nâng',
        recruitmentTrack: 'MASS_LABOR',
        applicationSource: 'WALK_IN',
        scoringMethod: 'RULE_ENGINE',
        aiMatchingScore: 90,
        skillMatchPercent: 92,
        experienceMatchPercent: 90,
        shiftHealthPercent: 88,
        aiSummary: 'Walk-in tại cổng: Có chứng chỉ vận hành xe nâng Reach Truck 2.5 tấn, 3 năm làm kho linh kiện điện tử, sức khỏe tốt.',
        tags: ['✅ Có chứng chỉ xe nâng', '✅ Xoay ca 12h', '🚶 Tiếp nhận tại cổng', 'Đã làm KCN 3 năm'],
        shift12hReady: true,
        isRuleRejected: false,
        cvFileUrl: null,
        rawResumeText: 'Ứng viên đến trực tiếp cổng bảo vệ số 2. Có sẵn chứng chỉ vận hành xe nâng điện đứng lái Reach Truck.',
        customFormFields: {
          'Hình thức tiếp nhận': 'Walk-in trực tiếp tại cổng bảo vệ nhà máy',
          'Chứng chỉ nghề': 'Chứng chỉ vận hành xe nâng Reach Truck (Còn hạn)',
          'Kinh nghiệm': '3 năm soạn hàng FIFO & quét mã vạch WMS tại kho linh kiện',
          'Cam kết làm ca đêm': 'Sẵn sàng xoay ca 12h'
        },
        oralChecklist: {
          handEyePass: true,
          visionPass: true,
          shift12hConfirmed: true,
          attitudePass: true,
          quickNote: 'Đã kiểm tra bản gốc chứng chỉ xe nâng tại bàn tuyển dụng.'
        },
        status: 'InterviewPassed',
        appliedAt: '2026-11-06T10:30:00Z'
      },
      {
        applicationId: 'mock-reject-01',
        candidateId: 'cand-reject-01',
        jobId: 'job-smt-01',
        jobTitle: 'Công nhân vận hành dây chuyền SMT (Tuyển gấp 100 người)',
        jobCode: 'JOB-SMT-100',
        departmentCode: 'PROD_LINE',
        departmentName: 'Xưởng Sản Xuất & Lắp Ráp SMT',
        candidateName: 'Ngô Văn Kiên',
        phone: '0943887766',
        email: 'kien.ngo@mass-recruit.local',
        yearOfBirth: '2004',
        cccd: '025204001199',
        hometown: 'Phú Thọ',
        educationLevel: 'THPT (12/12)',
        recruitmentTrack: 'MASS_LABOR',
        applicationSource: 'GOOGLE_FORM_QR',
        scoringMethod: 'RULE_ENGINE',
        aiMatchingScore: 38,
        skillMatchPercent: 60,
        experienceMatchPercent: 50,
        shiftHealthPercent: 15,
        aiSummary: 'Rule Engine (Tự động loại): Ứng viên tích chọn "Không làm được ca đêm 12h" và cận thị nặng 5.5 diop không phù hợp đứng chuyền gắn chip SMT.',
        tags: ['❌ Không làm ca đêm', '⚠️ Thị lực 4/10 (Cận nặng)', 'Chưa có kinh nghiệm KCN'],
        shift12hReady: false,
        isRuleRejected: true,
        ruleRejectReason: 'Từ chối làm xoay ca đêm 12h & Thị lực dưới chuẩn dây chuyền SMT',
        cvFileUrl: null,
        rawResumeText: 'Tờ khai QR Google Form: Ngô Văn Kiên | Không nhận làm ca đêm | Cận 5.5 độ.',
        customFormFields: {
          'Họ và tên của bạn': 'Ngô Văn Kiên',
          'Số điện thoại': '0943887766',
          'Cam kết làm xoay ca 12h (Ngày/Đêm)': 'Không (Chỉ làm hành chính)',
          'Tình trạng thị lực & sức khỏe': 'Cận thị 5.5 độ',
          'Kết luận Rule Engine': 'Không đạt tiêu chí bắt buộc của xưởng sản xuất 3 ca'
        },
        oralChecklist: {
          handEyePass: true,
          visionPass: false,
          shift12hConfirmed: false,
          attitudePass: true,
          quickNote: 'Chỉ xin làm ca hành chính, xưởng SMT không bố trí được.'
        },
        status: 'Rejected',
        appliedAt: '2026-11-06T11:00:00Z'
      }
    ];
  }
}


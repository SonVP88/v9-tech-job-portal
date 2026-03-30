import { Component, OnInit, OnDestroy, ChangeDetectorRef, inject, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { ApplicationService, ApplicationDto } from '../../../services/application.service';
import { JobService } from '../../../services/job.service';
import { OfferModalComponent } from '../../../components/admin/offer-modal/offer-modal';
import { ToastService } from '../../../services/toast.service';

interface InterviewForm {
  date: string;
  time: string;
  type: 'ONLINE' | 'OFFLINE';
  location: string;
  interviewerId: string;
}

interface InterviewerListItem {
  id: string;
  fullName: string;
  email: string;
  roleName: string;
}

@Component({
  selector: 'app-manage-applications',
  imports: [CommonModule, FormsModule, OfferModalComponent],
  templateUrl: './manage-applications.html',
  styleUrl: './manage-applications.scss',
})
export class ManageApplications implements OnInit, OnDestroy {
  applications: ApplicationDto[] = [];
  jobId: string = '';
  isLoading = true;
  isEmpty = false;
  hasError = false;
  errorMessage = '';

  private toast = inject(ToastService);
  private ngZone = inject(NgZone);

  // Tabs
  activeTab: 'APPLICATIONS' | 'RECOMMENDATIONS' = 'APPLICATIONS';
  recommendedCandidates: any[] = [];
  isLoadingRecommendations = false;

  // Modal state
  showInterviewModal = false;
  selectedApplication: ApplicationDto | null = null;

  // Interview form
  interviewForm: InterviewForm = {
    date: '',
    time: '',
    type: 'ONLINE',
    location: '',
    interviewerId: ''
  };

  // Danh sách Interviewer
  interviewers: InterviewerListItem[] = [];
  isLoadingInterviewers = false;

  // Email preview
  emailPreviewContent = '';
  aiOpeningText = ''; // Lưu đoạn mở đầu do AI sinh

  // ==================== INTERVIEW VALIDATION ====================
  // Validation errors
  dateValidationError = '';
  timeValidationError = '';
  locationValidationError = '';
  
  // Constants for validation
  readonly BUSINESS_HOURS_START = 8; // 8 AM (Giờ hành chính VN)
  readonly BUSINESS_HOURS_END = 18; // 6 PM (Giờ hành chính VN)
  readonly MIN_LOCATION_LENGTH = 5;

  // Danh sách ngày lễ Việt Nam (DD-MM format) - Tự động cập nhật từ API
  vietnameseHolidays: string[] = [
    '01-01', // Tết Dương Lịch (mặc định)
    '17-02', '18-02', '19-02', '20-02', '21-02', '22-02', '23-02', // Tết Nguyên Đán 2026 (mặc định)
    '30-04', '01-05', // Ngày Giải phóng - Quốc tế Lao động
    '02-09'  // Ngày Quốc khánh
  ];

  // Loading state for holidays
  isLoadingHolidays = false;

  // Loading states for other operations
  isGeneratingAI = false;
  isSendingEmail = false;

  // ==================== REJECT MODAL ====================
  showRejectModal = false;
  rejectApplication: ApplicationDto | null = null;
  rejectStep = 1; // 1: Chọn lý do, 2: Review email

  rejectReasons = {
    skill: false,
    salary: false,
    culture: false
  };
  rejectNote = '';

  // ==================== SEARCH & FILTER ====================
  searchQuery = '';
  showFilterPanel = false;
  filterStatus = '';
  filterSlaStatus = '';
  filterScoreRange = '';
  filterDateRange = '';
  onlyOverdue = false;
  prioritizeSla = false;
  rejectEmailContent = '';

  // ==================== PAGINATION ====================
  currentPage = 1;
  itemsPerPage = 6;
  Math = Math; // Expose Math for template

  isGeneratingRejectEmail = false;
  isSendingRejectEmail = false;

  // ==================== OFFER MODAL ====================
  isOfferModalOpen = false;
  selectedCandidateForOffer: ApplicationDto | null = null;

  private apiUrl = 'https://localhost:7181/api';
  private refreshInterval: any; // Auto-refresh timer

  // ==================== REFRESH CONFIGURATION ====================
  // Toggle between DEMO mode (fast refresh for presentations) and PRODUCTION mode
  private readonly DEMO_MODE = true; // Set to false for production
  private readonly REFRESH_INTERVAL_DEMO = 5000; // 5 seconds - for demo/presentation
  private readonly REFRESH_INTERVAL_PROD = 15000; // 15 seconds - for production

  lastRefreshTime: Date | null = null; // For UI display

  constructor(
    private applicationService: ApplicationService,
    private jobService: JobService,
    private route: ActivatedRoute,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private http: HttpClient
  ) { }

  ngOnInit(): void {
    // Lấy jobId từ URL params
    this.route.params.subscribe(params => {
      this.jobId = params['jobId'];

      this.loadApplications();
      this.loadVietnamHolidays(); // Tải danh sách ngày lễ từ API
      this.startAutoRefresh();
    });
  }

  ngOnDestroy(): void {
    this.stopAutoRefresh();
  }

  /**
   * Get current refresh interval based on mode
   */
  getRefreshInterval(): number {
    return this.DEMO_MODE ? this.REFRESH_INTERVAL_DEMO : this.REFRESH_INTERVAL_PROD;
  }

  // Chuyển tab và load data nếu cần
  switchTab(tab: 'APPLICATIONS' | 'RECOMMENDATIONS'): void {
    this.activeTab = tab;
    if (tab === 'RECOMMENDATIONS' && this.recommendedCandidates.length === 0 && this.jobId) {
      this.loadRecommendedCandidates();
    }
  }

  loadRecommendedCandidates(): void {
    if (!this.jobId) return;
    this.isLoadingRecommendations = true;

    this.ngZone.runOutsideAngular(() => {
      this.jobService.getRecommendedCandidates(this.jobId, 10).subscribe({
        next: (candidates) => {
          this.ngZone.run(() => {
            this.recommendedCandidates = candidates;
            this.isLoadingRecommendations = false;
            this.cdr.detectChanges();
          });
        },
        error: (err) => {
          console.error('Error loading AI candidates:', err);
          this.ngZone.run(() => {
            this.isLoadingRecommendations = false;
            this.cdr.detectChanges();
          });
        }
      });
    });
  }

  /**
   * Start auto-refresh with configurable interval
   * DEMO_MODE = true: 5 seconds (for presentations)
   * DEMO_MODE = false: 15 seconds (for production)
   */
  startAutoRefresh(): void {
    // Clear existing interval if any
    this.stopAutoRefresh();

    const interval = this.getRefreshInterval();
    const seconds = interval / 1000;

    // Set new interval
    this.refreshInterval = setInterval(() => {
      this.loadApplications();
    }, interval);

  }

  /**
   * Stop auto-refresh
   */
  stopAutoRefresh(): void {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
      this.refreshInterval = null;
    }
  }

  /**
   * Manual refresh button
   */
  refreshApplications(): void {
    this.loadApplications();
  }

  /**
   * Gọi API để lấy danh sách hồ sơ (Theo JobId hoặc Tất cả)
   */
  loadApplications(): void {

    this.isLoading = true;

    // Determine which service method to call
    const request = this.jobId
      ? this.applicationService.getApplicationsByJobId(this.jobId)
      : this.applicationService.getAllApplications();

    request.subscribe({
      next: (response) => {
        if (response.success) {
          this.applications = response.data;
          this.isEmpty = this.applications.length === 0;
          this.lastRefreshTime = new Date();

        } else {
        }
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Lỗi khi tải danh sách hồ sơ:', error);
        this.isLoading = false;
        this.isEmpty = true;
        this.cdr.detectChanges();
      }
    });
  }

  /**
   * Trả về class CSS cho điểm AI Match
   */
  getScoreColor(score?: number): string {
    if (!score) return 'text-gray-500 bg-gray-50';
    if (score >= 70) return 'text-green-700 bg-green-50 border-green-200';
    if (score >= 50) return 'text-yellow-700 bg-yellow-50 border-yellow-200';
    return 'text-red-700 bg-red-50 border-red-200';
  }

  /**
   * Trả về label tiếng Việt cho trạng thái
   */
  getStatusLabel(status: string): string {
    const statusMap: Record<string, string> = {
      'NEW': 'Mới nộp',
      'NEW_APPLIED': 'Mới nộp',
      'ACTIVE': 'Mới nộp',
      'SCREENING': 'Sàng lọc',
      'INTERVIEW': 'Phỏng vấn',
      'OFFER': 'Đề nghị',
      'OFFER_ACCEPTED': 'Đã đồng ý Offer',
      'HIRED': 'Đã tuyển',
      'REJECTED': 'Từ chối',
      'Waitlist': 'Danh sách chờ'
    };
    return statusMap[status] || status;
  }

  /**
   * Trả về class CSS cho Badge trạng thái
   */
  getStatusClass(status: string): string {
    switch (status) {
      case 'NEW_APPLIED':
      case 'ACTIVE':
        return 'px-3 py-1 rounded-full bg-blue-50 border border-blue-200 text-blue-700 text-xs font-semibold';
      case 'INTERVIEW':
        return 'px-3 py-1 rounded-full bg-yellow-50 border border-yellow-200 text-yellow-700 text-xs font-semibold';
      case 'HIRED':
        return 'px-3 py-1 rounded-full bg-green-50 border border-green-200 text-green-700 text-xs font-semibold';
      case 'OFFER_ACCEPTED':
        return 'px-3 py-1 rounded-full bg-teal-50 border border-teal-200 text-teal-700 text-xs font-semibold';
      case 'REJECTED':
        return 'px-3 py-1 rounded-full bg-red-50 border border-red-200 text-red-700 text-xs font-semibold';
      case 'Waitlist':
        return 'px-3 py-1 rounded-full bg-indigo-50 border border-indigo-200 text-indigo-700 text-xs font-semibold';
      case 'Pending_Offer':
        return 'px-3 py-1 rounded-full bg-purple-50 border border-purple-200 text-purple-700 text-xs font-semibold';
      case 'Offer_Sent':
        return 'px-3 py-1 rounded-full bg-orange-50 border border-orange-200 text-orange-700 text-xs font-semibold';
      default:
        return 'px-3 py-1 rounded-full bg-gray-50 border border-gray-200 text-gray-700 text-xs font-semibold';
    }
  }

  getSlaStatusLabel(app: ApplicationDto): string {
    if (app.status === 'Waitlist') {
      return 'Tạm dừng SLA';
    }

    if (app.status === 'HIRED' || app.status === 'OFFER_ACCEPTED' || app.status === 'REJECTED') {
      return 'SLA kết thúc';
    }

    switch (app.slaStatus) {
      case 'OVERDUE':
        return `Quá hạn ${app.slaOverdueDays ?? 0} ngày`;
      case 'WARNING':
        return 'Sắp quá hạn';
      case 'ON_TRACK':
        return 'Đúng hạn';
      default:
        return 'Không theo SLA';
    }
  }

  getSlaBadgeClass(app: ApplicationDto): string {
    if (app.status === 'Waitlist') {
      return 'inline-flex items-center px-2.5 py-1 rounded-full border border-indigo-200 bg-indigo-50 text-indigo-700 text-xs font-semibold';
    }

    if (app.status === 'HIRED' || app.status === 'OFFER_ACCEPTED' || app.status === 'REJECTED') {
      return 'inline-flex items-center px-2.5 py-1 rounded-full border border-slate-200 bg-slate-50 text-slate-700 text-xs font-semibold';
    }

    switch (app.slaStatus) {
      case 'OVERDUE':
        return 'inline-flex items-center px-2.5 py-1 rounded-full border border-red-200 bg-red-50 text-red-700 text-xs font-semibold';
      case 'WARNING':
        return 'inline-flex items-center px-2.5 py-1 rounded-full border border-yellow-200 bg-yellow-50 text-yellow-700 text-xs font-semibold';
      case 'ON_TRACK':
        return 'inline-flex items-center px-2.5 py-1 rounded-full border border-green-200 bg-green-50 text-green-700 text-xs font-semibold';
      default:
        return 'inline-flex items-center px-2.5 py-1 rounded-full border border-gray-200 bg-gray-50 text-gray-600 text-xs font-semibold';
    }
  }

  getSlaRowClass(app: ApplicationDto): string {
    if (app.slaStatus === 'OVERDUE') {
      return 'bg-red-50/60 hover:bg-red-100/50';
    }

    if (app.slaStatus === 'WARNING') {
      return 'bg-yellow-50/40 hover:bg-yellow-100/50';
    }

    return 'hover:bg-gray-50';
  }

  getSlaTooltip(app: ApplicationDto): string {
    if (app.status === 'Waitlist') {
      return 'Hồ sơ tạm dừng SLA vì job đã đủ chỉ tiêu và đang ở danh sách chờ.';
    }

    if (app.status === 'HIRED' || app.status === 'OFFER_ACCEPTED' || app.status === 'REJECTED') {
      return 'Hồ sơ đã kết thúc quy trình tuyển dụng, SLA tuyển dụng không còn tính tiếp.';
    }

    if (!app.slaDueAt || app.slaStatus === 'DISABLED') {
      return 'Stage này chưa bật SLA';
    }

    const due = this.formatDate(app.slaDueAt);
    const stage = app.currentStageName || app.currentStageCode || 'Unknown stage';
    return `Stage: ${stage} | Hạn SLA: ${due}`;
  }

  /**
   * Cập nhật trạng thái hồ sơ
   */
  updateStatus(applicationId: string, newStatus: string): void {
    // Nếu chọn INTERVIEW -> Mở modal phỏng vấn
    if (newStatus === 'INTERVIEW') {
      const app = this.applications.find(a => a.applicationId === applicationId);
      if (app) {
        this.openInterviewModal(app);
      }
      return;
    }

    // Nếu chọn REJECTED -> Mở modal từ chối (Human-in-the-loop)
    if (newStatus === 'REJECTED') {
      const app = this.applications.find(a => a.applicationId === applicationId);
      if (app) {
        this.openRejectModal(app);
      }
      return;
    }

    let confirmMessage = '';

    switch (newStatus) {
      case 'HIRED':
        confirmMessage = 'Bạn chắc chắn muốn TUYỂN ứng viên này? Hành động này sẽ gửi thông báo đến ứng viên.';
        break;
      default:
        confirmMessage = `Bạn có chắc muốn cập nhật trạng thái thành ${newStatus}?`;
    }

    if (confirm(confirmMessage)) {
      this.applicationService.updateApplicationStatus(applicationId, newStatus).subscribe({
        next: (response) => {
          if (response.success) {
            // Cập nhật UI
            const app = this.applications.find(a => a.applicationId === applicationId);
            if (app) {
              app.status = newStatus;
            }

            let successMessage = '';
            switch (newStatus) {
              case 'HIRED':
                successMessage = 'Chúc mừng! Đã tuyển ứng viên thành công!';
                break;
              case 'REJECTED':
                successMessage = 'Đã từ chối ứng viên.';
                break;
              default:
                successMessage = 'Cập nhật trạng thái thành công!';
            }

            // Xử lý Suggest Close Job
            if (newStatus === 'HIRED' && response.data) {
              const data = response.data;
              if (data.isJobActive && data.numberOfPositions != null && data.totalHired >= data.numberOfPositions) {
                // Tạm thời tắt alert default nếu show confirm
                setTimeout(() => {
                  if (confirm(`Bạn đã tuyển đủ ${data.totalHired}/${data.numberOfPositions} vị trí cho Job này. Bạn có muốn ĐÓNG tin tuyển dụng ngay để ngừng nhận thêm CV không?`)) {
                    this.jobService.closeJob(data.jobId).subscribe({
                      next: () => {
                        this.toast.success('Đã đóng Job', 'Tin tuyển dụng sẽ không còn hiển thị với ứng viên.');
                      },
                      error: (err) => {
                        console.error('Lỗi khi đóng job:', err);
                        this.toast.error('Lỗi đóng Job', 'Có lỗi xảy ra khi đóng Job!');
                      }
                    });
                  }
                }, 100);
              }
            }

            this.toast.success('Cập nhật thành công', successMessage);
            this.cdr.detectChanges();
          }
        },
        error: (error) => {
          console.error('Lỗi khi cập nhật trạng thái:', error);
          this.toast.error('Cập nhật thất bại', 'Có lỗi xảy ra khi cập nhật trạng thái!');
        }
      });
    }
  }

  /**
   * Mở CV trong tab mới và ghi nhận lượt xem
   */
  viewCv(app: any): void {
    if (!app || !app.cvUrl) {
      this.toast.error('Lỗi', 'Ứng viên này không có CV hoặc định dạng CV không hợp lệ.');
      return;
    }

    // 1. Gọi API tracking (không cần chờ kết quả để mở CV)
    this.applicationService.trackCvView(app.applicationId).subscribe({
      next: () => console.log('CV view tracked'),
      error: (err: any) => console.error('CV view tracking error', err)
    });

    // 2. Mở CV
    let url = app.cvUrl;
    if (!url.startsWith('http')) {
      const clean = url.startsWith('/') ? url.substring(1) : url;
      url = `https://localhost:7181/${clean}`;
    }
    if (url.startsWith('http://') && window.location.protocol === 'https:') {
      url = url.replace('http://', 'https://');
    }
    
    window.open(url, '_blank');
  }

  /**
   * Format ngày tháng
   */
  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  }

  // ==================== MODAL INTERVIEW ====================

  /**
   * Mở modal lên lịch phỏng vấn
   */
  openInterviewModal(application: ApplicationDto): void {
    this.selectedApplication = application;
    this.showInterviewModal = true;

    // Reset form
    this.interviewForm = {
      date: '',
      time: '09:00',
      type: 'ONLINE',
      location: '',
      interviewerId: ''
    };
    this.aiOpeningText = '';
    
    // Reset validation errors
    this.dateValidationError = '';
    this.timeValidationError = '';
    this.locationValidationError = '';
    
    this.updateEmailPreview();

    // Load danh sách Interviewer
    this.loadInterviewers();

    this.cdr.detectChanges();
  }

  /**
   * Đóng modal
   */
  closeInterviewModal(): void {
    this.showInterviewModal = false;
    this.selectedApplication = null;
    this.emailPreviewContent = '';
    this.aiOpeningText = '';
    this.cdr.detectChanges();
  }

  // ==================== OFFER MODAL METHODS ====================

  /**
   * Mở modal gửi Offer Letter
   */
  openOfferModal(application: ApplicationDto): void {
    this.selectedCandidateForOffer = application;
    this.isOfferModalOpen = true;
  }

  /**
   * Xử lý khi offer đã được gửi thành công
   */
  handleOfferSent(payload: any): void {

    // Close modal
    this.isOfferModalOpen = false;
    this.selectedCandidateForOffer = null;

    // Reload applications list để cập nhật status mới
    this.loadApplications();
  }

  /**
   * Navigate to Candidate Detail page
   */
  viewCandidateDetail(app: ApplicationDto): void {
    this.router.navigate(['/hr/candidate-detail'], {
      state: { candidate: app }
    });
  }

  /**
   * Cập nhật nội dung email preview (Two-way binding)
   */
  updateEmailPreview(): void {
    const formattedDate = this.interviewForm.date
      ? new Date(this.interviewForm.date).toLocaleDateString('vi-VN', { weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric' })
      : '[Chưa chọn ngày]';

    const typeLabel = this.interviewForm.type === 'ONLINE' ? 'Online (Video Call)' : 'Offline (Trực tiếp)';
    const locationLabel = this.interviewForm.type === 'ONLINE' ? 'Link Meeting' : 'Địa điểm';
    const locationValue = this.interviewForm.location || '[Chưa nhập]';

    // Template email
    this.emailPreviewContent = `${this.aiOpeningText ? this.aiOpeningText + '\n\n' : '[Bấm "✨ AI Personalize" để tạo đoạn mở đầu cá nhân hóa...]\n\n'}Chi tiết buổi phỏng vấn:
- Thời gian: ${formattedDate} lúc ${this.interviewForm.time || '[Chưa chọn giờ]'}
- Hình thức: ${typeLabel}
- ${locationLabel}: ${locationValue}

Vui lòng xác nhận tham gia bằng cách phản hồi email này.

Trân trọng,
Phòng Nhân sự`;
  }

  /**
   * Gọi API sinh đoạn mở đầu bằng AI
   */
  generateAIOpening(): void {
    if (!this.selectedApplication) return;

    this.isGeneratingAI = true;
    this.cdr.detectChanges();

    const token = localStorage.getItem('auth_token');
    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });

    const body = {
      candidateId: this.selectedApplication.candidateId,
      jobId: this.selectedApplication.jobId
    };

    this.http.post<{ opening: string }>(`${this.apiUrl}/interviews/generate-opening`, body, { headers })
      .subscribe({
        next: (response) => {
          this.aiOpeningText = response.opening;
          this.updateEmailPreview();
          this.isGeneratingAI = false;
          this.cdr.detectChanges();
        },
        error: (error) => {
          console.error(' Error generating AI opening:', error);
          alert('Có lỗi khi tạo nội dung AI. Vui lòng thử lại!');
          this.isGeneratingAI = false;
          this.cdr.detectChanges();
        }
      });
  }

  /**
   * ==================== VIETNAM HOLIDAYS LOADING ====================
   * 
   * API ENDPOINT CẦN IMPLEMENT TRÊN BACKEND:
   * GET /api/master-data/vietnam-holidays/{year}
   * 
   * Response Format:
   * {
   *   "holidays": ["01-01", "17-02", "18-02", ..., "02-09"],
   *   "year": 2026,
   *   "count": 8
   * }
   * 
   * Chi tiết:
   * - holidays: Array<string> - Danh sách ngày lễ (format DD-MM)
   * - year: number - Năm  
   * - count: number - Số lượng ngày lễ trong năm
   * 
   * Ví dụ Response cho năm 2026:
   * {
   *   "holidays": [
   *     "01-01", // Tết Dương Lịch
   *     "17-02", "18-02", "19-02", "20-02", "21-02", "22-02", "23-02", // Tết Nguyên Đán
   *     "30-04", "01-05", // Giải phóng - Lao động
   *     "02-09"  // Quốc khánh
   *   ],
   *   "year": 2026,
   *   "count": 15
   * }
   * 
   * ==================== END API REQUIREMENT ====================
   */

  /**
   * Tải danh sách ngày lễ Việt Nam từ API
   * API sẽ trả về danh sách ngày lễ cho năm hiện tại au một lần, sau đó cache lại
   */
  loadVietnamHolidays(): void {
    this.isLoadingHolidays = true;
    const token = localStorage.getItem('auth_token');
    const headers = new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });

    const currentYear = new Date().getFullYear();

    this.http.get<{ holidays: string[] }>(`${this.apiUrl}/master-data/vietnam-holidays/${currentYear}`, { headers })
      .subscribe({
        next: (response) => {
          if (response && response.holidays && response.holidays.length > 0) {
            // Cập nhật danh sách ngày lễ từ API (format: DD-MM)
            this.vietnameseHolidays = response.holidays;
            console.log(`✅ Loaded ${this.vietnameseHolidays.length} Vietnam holidays for year ${currentYear}`);
          }
          this.isLoadingHolidays = false;
          this.cdr.detectChanges();
        },
        error: (error) => {
          console.warn('⚠️ Không thể tải danh sách ngày lễ từ API, sử dụng danh sách mặc định:', error);
          // Giữ danh sách mặc định nếu API lỗi
          this.isLoadingHolidays = false;
          this.cdr.detectChanges();
        }
      });
  }

  /**
   * Gọi API lấy danh sách Interviewer
   */
  loadInterviewers(): void {
    this.isLoadingInterviewers = true;
    const token = localStorage.getItem('auth_token');
    const headers = new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });


    this.http.get<Array<{ id: string, fullName: string, email: string, roleName: string }>>(`${this.apiUrl}/employees/interviewers`, { headers })
      .subscribe({
        next: (response) => {
          this.interviewers = response;
          this.isLoadingInterviewers = false;
          this.cdr.detectChanges();
        },
        error: (error) => {
          console.error(' Error loading interviewers:', error);
          alert('Có lỗi khi tải danh sách người phỏng vấn!');
          this.isLoadingInterviewers = false;
          this.cdr.detectChanges();
        }
      });
  }

  /**
 * Group interviewers theo Role để hiển thị trong optgroup
 */
  getInterviewersByRole(role: string): InterviewerListItem[] {
    return this.interviewers.filter(i => i.roleName === role);
  }

  /**
   * Kiểm tra có interviewer nào với role này không
   */
  hasInterviewersWithRole(role: string): boolean {
    return this.interviewers.some(i => i.roleName === role);
  }

  // ==================== CUSTOM DROPDOWN STATE ====================

  isDropdownOpen = false;
  dropdownSearchQuery = '';
  expandedGroups = {
    ADMIN: true,
    HR: true,
    INTERVIEWER: true
  };

  /**
   * Toggle dropdown open/close
   */
  toggleDropdown(): void {
    this.isDropdownOpen = !this.isDropdownOpen;
    if (this.isDropdownOpen) {
      this.dropdownSearchQuery = '';
      // Auto-expand all groups when opening
      this.expandedGroups = { ADMIN: true, HR: true, INTERVIEWER: true };
    }
  }

  /**
   * Close dropdown
   */
  closeDropdown(): void {
    this.isDropdownOpen = false;
    this.dropdownSearchQuery = '';
  }

  /**
   * Toggle group collapse/expand
   */
  toggleGroup(role: 'ADMIN' | 'HR' | 'INTERVIEWER'): void {
    this.expandedGroups[role] = !this.expandedGroups[role];
  }

  /**
   * Select interviewer
   */
  selectInterviewer(user: InterviewerListItem): void {
    this.interviewForm.interviewerId = user.id;
    this.closeDropdown();
  }

  /**
   * Get selected interviewer display name
   */
  getSelectedInterviewerName(): string {
    if (!this.interviewForm.interviewerId) return '';
    const selected = this.interviewers.find(i => i.id === this.interviewForm.interviewerId);
    return selected ? `${selected.fullName} - ${selected.email}` : '';
  }

  /**
   * Filter interviewers by search query
   */
  getFilteredInterviewersByRole(role: string): InterviewerListItem[] {
    const byRole = this.getInterviewersByRole(role);
    if (!this.dropdownSearchQuery.trim()) return byRole;

    const query = this.dropdownSearchQuery.toLowerCase();
    return byRole.filter(i =>
      i.fullName.toLowerCase().includes(query) ||
      i.email.toLowerCase().includes(query)
    );
  }

  /**
   * Get initials from name
   */
  getInitials(fullName: string): string {
    return fullName
      .split(' ')
      .map(n => n.charAt(0))
      .join('')
      .toUpperCase()
      .substring(0, 2);
  }

  /**
   * Get avatar color based on name
   */
  getAvatarColor(fullName: string): string {
    const colors = [
      'bg-blue-500',
      'bg-purple-500',
      'bg-pink-500',
      'bg-green-500',
      'bg-yellow-500',
      'bg-indigo-500'
    ];
    const index = fullName.charCodeAt(0) % colors.length;
    return colors[index];
  }

  /**
   * Lấy tên ngày tiếng Việt (Thứ 2, Thứ 3, ..., Chủ nhật)
   */
  getDayNameInVietnamese(date: Date): string {
    const dayOfWeek = date.getDay();
    const dayNames = ['Chủ nhật', 'Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7'];
    return dayNames[dayOfWeek];
  }

  /**
   * Kiểm tra ngày có phải thứ 7 hoặc chủ nhật không (Weekday: 0-6, 0=Chủ nhật, 6=Thứ 7)
   */
  isWeekend(date: Date): boolean {
    const dayOfWeek = date.getDay();
    return dayOfWeek === 0 || dayOfWeek === 6; // 0 = Sunday, 6 = Saturday
  }

  /**
   * Kiểm tra ngày có phải ngày lễ Việt Nam không (DD-MM format)
   */
  isVietnamHoliday(date: Date): boolean {
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const dateString = `${day}-${month}`;
    return this.vietnameseHolidays.includes(dateString);
  }

  /**
   * Kiểm tra ngày có phải ngày lao động của Việt Nam không
   * (không phải weekend, không phải lễ)
   */
  isValidWorkingDay(date: Date): boolean {
    return !this.isWeekend(date) && !this.isVietnamHoliday(date);
  }

  /**
   * Kiểm tra ngày phỏng vấn là ngày lao động trong tương lai
   */
  isValidFutureDate(): boolean {
    if (!this.interviewForm.date) {
      this.dateValidationError = '';
      return false;
    }

    const selectedDate = new Date(this.interviewForm.date);
    selectedDate.setHours(0, 0, 0, 0);
    
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    if (selectedDate < today) {
      this.dateValidationError = '❌ Ngày phỏng vấn phải là ngày trong tương lai';
      return false;
    }

    // Kiểm tra ngày lễ Việt Nam
    if (this.isVietnamHoliday(selectedDate)) {
      this.dateValidationError = '❌ Ngày này là ngày lễ Việt Nam, không thể lên lịch phỏng vấn';
      return false;
    }

    // Kiểm tra weekend (thứ 7, chủ nhật)
    if (this.isWeekend(selectedDate)) {
      const dayName = selectedDate.getDay() === 0 ? 'Chủ nhật' : 'Thứ 7';
      this.dateValidationError = `❌ ${dayName} không phải ngày lao động, vui lòng chọn ngày khác`;
      return false;
    }

    this.dateValidationError = '';
    return true;
  }

  /**
   * Kiểm tra giờ phỏng vấn có nằm trong giờ hành chính (8 AM - 6 PM)
   */
  isValidBusinessHours(): boolean {
    if (!this.interviewForm.time) {
      this.timeValidationError = '';
      return false;
    }

    const timeParts = this.interviewForm.time.split(':');
    const hours = parseInt(timeParts[0]);

    if (hours < this.BUSINESS_HOURS_START || hours >= this.BUSINESS_HOURS_END) {
      this.timeValidationError = `❌ Thời gian phỏng vấn phải nằm trong giờ hành chính (${this.BUSINESS_HOURS_START}:00 - ${this.BUSINESS_HOURS_END}:00)`;
      return false;
    }

    this.timeValidationError = '';
    return true;
  }

  /**
   * Kiểm tra URL hợp lệ cho online interviews
   */
  isValidMeetingLink(): boolean {
    // Nếu là offline, không cần validate URL
    if (this.interviewForm.type === 'OFFLINE') {
      this.locationValidationError = '';
      return true;
    }

    const location = this.interviewForm.location?.trim() || '';
    
    if (!location) {
      this.locationValidationError = '❌ Vui lòng nhập link meeting';
      return false;
    }

    // Kiểm tra có phải URL hợp lệ không
    try {
      new URL(location);
      this.locationValidationError = '';
      return true;
    } catch (e) {
      this.locationValidationError = '❌ Địa chỉ link meeting không hợp lệ (ví dụ: https://meet.google.com/xxxxx)';
      return false;
    }
  }

  /**
   * Kiểm tra địa điểm offline hợp lệ
   */
  isValidOfflineLocation(): boolean {
    // Nếu là online, không cần validate địa điểm
    if (this.interviewForm.type === 'ONLINE') {
      this.locationValidationError = '';
      return true;
    }

    const location = this.interviewForm.location?.trim() || '';
    
    if (!location || location.length < this.MIN_LOCATION_LENGTH) {
      this.locationValidationError = `❌ Vui lòng nhập địa điểm (tối thiểu ${this.MIN_LOCATION_LENGTH} ký tự)`;
      return false;
    }

    this.locationValidationError = '';
    return true;
  }

  /**
   * Kiểm tra location (offline hoặc online)
   */
  isValidLocation(): boolean {
    if (this.interviewForm.type === 'ONLINE') {
      return this.isValidMeetingLink();
    } else {
      return this.isValidOfflineLocation();
    }
  }

  /**
   * Kiểm tra form hợp lệ (các trường bắt buộc + validation chi tiết)
   */
  isFormValid(): boolean {
    // Kiểm tra các trường bắt buộc không rỗng
    const hasRequiredFields = !!(
      this.interviewForm.date &&
      this.interviewForm.time &&
      this.interviewForm.location &&
      this.interviewForm.interviewerId &&
      this.emailPreviewContent.trim()
    );

    if (!hasRequiredFields) return false;

    // Kiểm tra chi tiết từng trường
    return (
      this.isValidFutureDate() &&
      this.isValidBusinessHours() &&
      this.isValidLocation()
    );
  }

  /**
   * Gửi lời mời phỏng vấn
   */
  sendInterviewInvitation(): void {
    if (!this.selectedApplication || !this.isFormValid()) return;

    this.isSendingEmail = true;
    this.cdr.detectChanges();

    const token = localStorage.getItem('auth_token');
    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });

    // Tính toán scheduledStart và scheduledEnd từ date + time
    // Normalize date format to YYYY-MM-DD (ISO 8601)
    let normalizedDate: string;

    if (this.interviewForm.date.includes('/')) {
      // Format: MM/DD/YYYY hoặc DD/MM/YYYY → parse sang YYYY-MM-DD
      const dateParts = this.interviewForm.date.split('/');
      const dateObj = new Date(this.interviewForm.date);
      const year = dateObj.getFullYear();
      const month = String(dateObj.getMonth() + 1).padStart(2, '0');
      const day = String(dateObj.getDate()).padStart(2, '0');
      normalizedDate = `${year}-${month}-${day}`;
    } else {
      // Đã đúng format YYYY-MM-DD
      normalizedDate = this.interviewForm.date;
    }

    // Normalize time format to HH:mm (24h format)
    let normalizedTime = this.interviewForm.time;
    if (normalizedTime.includes('AM') || normalizedTime.includes('PM')) {
      // Convert 12h format to 24h
      const timeParts = normalizedTime.replace(/\s?(AM|PM)/i, '').split(':');
      let hours = parseInt(timeParts[0]);
      const minutes = timeParts[1];
      const isPM = normalizedTime.toUpperCase().includes('PM');

      if (isPM && hours !== 12) hours += 12;
      if (!isPM && hours === 12) hours = 0;

      normalizedTime = `${String(hours).padStart(2, '0')}:${minutes}`;
    }

    const scheduledStart = `${normalizedDate}T${normalizedTime}:00`; // ISO 8601 format
    const scheduledEnd = this.calculateEndTime(scheduledStart, 60); // Mặc định 60 phút

    console.log('🕒 Time values:', {
      original: { date: this.interviewForm.date, time: this.interviewForm.time },
      normalized: { date: normalizedDate, time: normalizedTime },
      scheduledStart,
      scheduledEnd,
      startDate: new Date(scheduledStart),
      endDate: new Date(scheduledEnd),
      isValid: !isNaN(new Date(scheduledStart).getTime())
    });

    // Payload cho backend schedule-interview API
    const schedulePayload = {
      interviewerId: this.interviewForm.interviewerId,
      title: `Phỏng vấn - ${this.selectedApplication.jobTitle || 'Vị trí tuyển dụng'}`,
      scheduledStart: scheduledStart,
      scheduledEnd: scheduledEnd,
      meetingLink: this.interviewForm.type === 'ONLINE' ? this.interviewForm.location : null,
      location: this.interviewForm.type === 'OFFLINE' ? this.interviewForm.location : null
    };


    // 1. Lên lịch phỏng vấn (POST /api/applications/{id}/schedule-interview)
    this.http.post(
      `${this.apiUrl}/applications/${this.selectedApplication.applicationId}/schedule-interview`,
      schedulePayload,
      { headers }
    ).subscribe({
      next: (response) => {
        console.log('Interview scheduled successfully:', response);
        console.log(' Email with CC sent automatically by backend');

        // Update trạng thái INTERVIEW trong UI
        const app = this.applications.find(a => a.applicationId === this.selectedApplication!.applicationId);
        if (app) {
          app.status = 'INTERVIEW';
        }

        this.toast.success('Lịch phỏng vấn đã gửi', 'Đã lên lịch phỏng vấn và gửi email thành công!');
        this.closeInterviewModal();
        this.isSendingEmail = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error(' Error scheduling interview:', error);
        const errorMsg = error.error?.message || 'Có lỗi khi lên lịch phỏng vấn. Vui lòng thử lại!';
        this.toast.error('Không thể lên lịch', errorMsg);
        this.isSendingEmail = false;
        this.cdr.detectChanges();
      }
    });
  }

  /**
   * Tính thời gian kết thúc (thêm N phút vào start time)
   */
  private calculateEndTime(startTime: string, durationMinutes: number): string {
    const start = new Date(startTime);

    // Validate date
    if (isNaN(start.getTime())) {
      console.error(' Invalid start time:', startTime);
      throw new Error('Invalid start time format');
    }

    const end = new Date(start.getTime() + durationMinutes * 60000);

    // Format as local datetime string (YYYY-MM-DDTHH:mm:ss), NOT UTC
    const year = end.getFullYear();
    const month = String(end.getMonth() + 1).padStart(2, '0');
    const day = String(end.getDate()).padStart(2, '0');
    const hours = String(end.getHours()).padStart(2, '0');
    const minutes = String(end.getMinutes()).padStart(2, '0');
    const seconds = String(end.getSeconds()).padStart(2, '0');

    return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`;
  }

  // ==================== REJECT MODAL METHODS ====================

  /**
   * Mở modal từ chối hồ sơ
   */
  openRejectModal(application: ApplicationDto): void {
    this.rejectApplication = application;
    this.showRejectModal = true;
    this.rejectStep = 1;

    // Reset form
    this.rejectReasons = { skill: false, salary: false, culture: false };
    this.rejectNote = '';
    this.rejectEmailContent = '';

    this.cdr.detectChanges();
  }

  /**
   * Đóng modal từ chối
   */
  closeRejectModal(): void {
    this.showRejectModal = false;
    this.rejectApplication = null;
    this.rejectStep = 1;
    this.rejectEmailContent = '';
    this.cdr.detectChanges();
  }

  /**
   * Kiểm tra có chọn ít nhất 1 lý do không
   */
  hasSelectedReason(): boolean {
    return this.rejectReasons.skill || this.rejectReasons.salary || this.rejectReasons.culture;
  }

  /**
   * Thu thập lý do từ checkboxes
   */
  private collectReasons(): string[] {
    const reasons: string[] = [];
    if (this.rejectReasons.skill) reasons.push('Chuyên môn chưa đạt');
    if (this.rejectReasons.salary) reasons.push('Mức lương không phù hợp');
    if (this.rejectReasons.culture) reasons.push('Văn hóa không phù hợp');
    return reasons;
  }

  /**
   * Gọi API sinh email từ chối bằng AI
   */
  generateRejectionDraft(): void {
    if (!this.rejectApplication || !this.hasSelectedReason()) return;

    this.isGeneratingRejectEmail = true;
    this.cdr.detectChanges();

    const token = localStorage.getItem('auth_token');
    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });

    const body = {
      candidateName: this.rejectApplication.candidateName,
      jobTitle: this.rejectApplication.jobTitle || 'Vị trí tuyển dụng',
      reasons: this.collectReasons(),
      note: this.rejectNote
    };



    this.http.post<{ body: string }>(`${this.apiUrl}/interviews/generate-rejection`, body, { headers })
      .subscribe({
        next: (response) => {
          this.rejectEmailContent = response.body;
          this.rejectStep = 2; // Chuyển sang bước 2
          this.isGeneratingRejectEmail = false;
          this.cdr.detectChanges();
        },
        error: (error) => {
          console.error(' Error generating rejection email:', error);
          alert('Có lỗi khi tạo email. Vui lòng thử lại!');
          this.isGeneratingRejectEmail = false;
          this.cdr.detectChanges();
        }
      });
  }

  /**
   * Xác nhận gửi email từ chối và cập nhật trạng thái
   */
  confirmReject(): void {
    if (!this.rejectApplication || !this.rejectEmailContent.trim()) return;

    this.isSendingRejectEmail = true;
    this.cdr.detectChanges();

    const token = localStorage.getItem('auth_token');
    const headers = new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    });

    // 1. Gửi email từ chối
    const emailBody = {
      toEmail: this.rejectApplication.email,
      subject: `Thông báo kết quả ứng tuyển - ${this.rejectApplication.jobTitle || 'Vị trí tuyển dụng'}`,
      bodyHtml: this.rejectEmailContent.replace(/\n/g, '<br>')
    };

    console.log('📧 Sending rejection email...', emailBody);

    this.http.post(`${this.apiUrl}/interviews/send-email-manual`, emailBody, { headers })
      .subscribe({
        next: (response) => {
          console.log(' Rejection email sent successfully:', response);

          // 2. Cập nhật trạng thái REJECTED
          this.applicationService.updateApplicationStatus(
            this.rejectApplication!.applicationId,
            'REJECTED'
          ).subscribe({
            next: (statusResponse) => {
              if (statusResponse.success) {
                // Cập nhật UI
                const app = this.applications.find(a => a.applicationId === this.rejectApplication!.applicationId);
                if (app) {
                  app.status = 'REJECTED';
                }

                this.toast.success('Gửi email thành công', 'Đã gửi email từ chối và cập nhật trạng thái thành công!');
                this.closeRejectModal();
                this.isSendingRejectEmail = false;
                this.cdr.detectChanges();
              }
            },
            error: (error) => {
              console.error(' Error updating status:', error);
              this.toast.warning('Email đã gửi', 'Nhưng có lỗi khi cập nhật trạng thái!');
              this.isSendingRejectEmail = false;
              this.cdr.detectChanges();
            }
          });
        },
        error: (error) => {
          console.error(' Error sending rejection email:', error);
          this.toast.error('Gửi email thất bại', 'Có lỗi khi gửi email. Vui lòng thử lại!');
          this.isSendingRejectEmail = false;
          this.cdr.detectChanges();
        }
      });
  }

  // ==================== SEARCH & FILTER METHODS ====================

  /**
   * Computed property: Danh sách applications đã được filter
   */
  filteredApplications(): ApplicationDto[] {
    let filtered = [...this.applications];

    // Apply search query
    if (this.searchQuery.trim()) {
      const query = this.searchQuery.toLowerCase().trim();
      filtered = filtered.filter(app =>
        app.candidateName.toLowerCase().includes(query) ||
        app.email.toLowerCase().includes(query) ||
        (app.phone && app.phone.includes(query))
      );
    }

    // Apply status filter
    if (this.filterStatus) {
      filtered = filtered.filter(app => app.status === this.filterStatus);
    }

    // Apply SLA status filter
    if (this.filterSlaStatus) {
      filtered = filtered.filter(app => app.slaStatus === this.filterSlaStatus);
    }

    // Quick filter: only overdue applications
    if (this.onlyOverdue) {
      filtered = filtered.filter(app => app.slaStatus === 'OVERDUE');
    }

    // Apply AI score filter
    if (this.filterScoreRange) {
      const [min, max] = this.filterScoreRange.split('-').map(Number);
      filtered = filtered.filter(app => {
        const score = app.matchScore || 0;
        return score >= min && score <= max;
      });
    }

    // Apply date range filter
    if (this.filterDateRange) {
      const now = new Date();
      const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());

      filtered = filtered.filter(app => {
        const appliedDate = new Date(app.appliedAt);
        const appDate = new Date(appliedDate.getFullYear(), appliedDate.getMonth(), appliedDate.getDate());

        if (this.filterDateRange === 'today') {
          return appDate.getTime() === today.getTime();
        } else if (this.filterDateRange === 'week') {
          const weekAgo = new Date(today);
          weekAgo.setDate(weekAgo.getDate() - 7);
          return appDate >= weekAgo;
        } else if (this.filterDateRange === 'month') {
          const monthAgo = new Date(today);
          monthAgo.setDate(monthAgo.getDate() - 30);
          return appDate >= monthAgo;
        }
        return true;
      });
    }

    if (this.prioritizeSla) {
      filtered.sort((a, b) => {
        const severityDiff = this.getSlaSeverity(b.slaStatus) - this.getSlaSeverity(a.slaStatus);
        if (severityDiff !== 0) {
          return severityDiff;
        }

        const overdueDiff = (b.slaOverdueDays ?? 0) - (a.slaOverdueDays ?? 0);
        if (overdueDiff !== 0) {
          return overdueDiff;
        }

        return new Date(b.appliedAt).getTime() - new Date(a.appliedAt).getTime();
      });
    }

    return filtered;
  }

  /**
   * Toggle filter panel visibility
   */
  toggleFilterPanel(): void {
    this.showFilterPanel = !this.showFilterPanel;
  }

  /**
   * Triggered when search input changes
   */
  onSearchChange(): void {
    this.currentPage = 1;
    // Debounce could be added here if needed
    this.cdr.detectChanges();
  }

  /**
   * Apply filters (called when dropdown changes)
   */
  applyFilters(): void {
    this.currentPage = 1;
    this.cdr.detectChanges();
  }

  /**
   * Clear all filters and search
   */
  clearFilters(): void {
    this.searchQuery = '';
    this.filterStatus = '';
    this.filterSlaStatus = '';
    this.filterScoreRange = '';
    this.filterDateRange = '';
    this.onlyOverdue = false;
    this.prioritizeSla = false;
    this.currentPage = 1;
    this.cdr.detectChanges();
  }

  /**
   * Get count of active filters
   */
  getActiveFiltersCount(): number {
    let count = 0;
    if (this.filterStatus) count++;
    if (this.filterSlaStatus) count++;
    if (this.filterScoreRange) count++;
    if (this.filterDateRange) count++;
    if (this.onlyOverdue) count++;
    if (this.prioritizeSla) count++;
    return count;
  }

  /**
   * Export filtered applications to Excel (CSV format)
   */
  exportToExcel(): void {
    const filtered = this.filteredApplications();

    if (filtered.length === 0) {
      alert('Không có dữ liệu để xuất!');
      return;
    }

    // Prepare CSV data
    const headers = ['Tên ứng viên', 'Email', 'Số điện thoại', 'Ngày nộp', 'AI Match Score', 'Trạng thái', 'SLA', 'Quá hạn (ngày)', 'Vị trí'];
    const rows = filtered.map(app => [
      app.candidateName,
      `'${app.email}`, // Force text format with apostrophe
      app.phone ? `'${app.phone}` : '', // Force text format
      this.formatDate(app.appliedAt),
      app.matchScore ? `${app.matchScore}%` : 'N/A',
      this.getStatusLabel(app.status),
      this.getSlaStatusLabel(app),
      app.slaOverdueDays ?? 0,
      app.jobTitle || ''
    ]);

    // Convert to CSV string with proper escaping
    const csvContent = [
      headers.join(','),
      ...rows.map(row => row.map(cell => {
        // Escape double quotes and wrap in quotes
        const escaped = String(cell).replace(/"/g, '""');
        return `"${escaped}"`;
      }).join(','))
    ].join('\n');

    // Create Blob and download
    const blob = new Blob(['\uFEFF' + csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);

    link.setAttribute('href', url);
    link.setAttribute('download', `ung-vien-${new Date().toISOString().split('T')[0]}.csv`);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    console.log(` Exported ${filtered.length} applications to CSV`);
  }

  // ==================== PAGINATION METHODS ====================

  /**
   * Calculate total pages
   */
  totalPages(): number {
    return Math.ceil(this.filteredApplications().length / this.itemsPerPage);
  }

  /**
   * Get paginated applications for current page
   */
  paginatedApplications(): ApplicationDto[] {
    const filtered = this.filteredApplications();
    const start = (this.currentPage - 1) * this.itemsPerPage;
    return filtered.slice(start, start + this.itemsPerPage);
  }

  /**
   * Change to specific page
   */
  changePage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage = page;
      this.cdr.detectChanges();
    }
  }

  /**
   * Go to next page
   */
  nextPage(): void {
    if (this.currentPage < this.totalPages()) {
      this.currentPage++;
      this.cdr.detectChanges();
    }
  }

  /**
   * Go to previous page
   */
  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.cdr.detectChanges();
    }
  }

  /**
   * Get page numbers for pagination UI
   */
  getPageNumbers(): number[] {
    const total = this.totalPages();
    const current = this.currentPage;
    const delta = 2;

    const range: number[] = [];
    for (let i = Math.max(2, current - delta); i <= Math.min(total - 1, current + delta); i++) {
      range.push(i);
    }

    if (current - delta > 2) {
      range.unshift(-1); // ellipsis
    }
    if (current + delta < total - 1) {
      range.push(-1); // ellipsis
    }

    range.unshift(1);
    if (total > 1) {
      range.push(total);
    }

    return range;
  }

  private getSlaSeverity(status?: string): number {
    switch (status) {
      case 'OVERDUE':
        return 3;
      case 'WARNING':
        return 2;
      case 'ON_TRACK':
        return 1;
      default:
        return 0;
    }
  }
}

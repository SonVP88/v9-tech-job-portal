import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Component, OnInit, ChangeDetectorRef, ChangeDetectionStrategy, inject, PLATFORM_ID } from '@angular/core';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { InterviewService, MyInterviewDto } from '../../services/interview.service';
import { EvaluationService, EvaluationSubmitDto, EvaluationDetail } from '../../services/evaluation.service';
import { AuthService } from '../../services/auth.service';
import { ToastService } from '../../services/toast.service';

/**
 * Interface định nghĩa UI State cho từng interview
 */
export interface InterviewState {
  badgeClass: string;
  badgeLabel: string;
  buttonText: string;
  buttonClass: string;
  isButtonDisabled: boolean;
  isOverdue: boolean;
  statusType: 'completed' | 'overdue' | 'ready' | 'upcoming';
}

@Component({
  selector: 'app-my-interviews',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './my-interviews.html',
  styleUrl: './my-interviews.scss',
  // changeDetection: ChangeDetectionStrategy.OnPush //  Tạm thời tắt OnPush để fix UI issues
})
export class MyInterviews implements OnInit {
  interviews: MyInterviewDto[] = [];
  allInterviews: MyInterviewDto[] = [];
  paginatedInterviews: MyInterviewDto[] = []; // 📄 Pagination: Displayed items

  filterType: 'upcoming' | 'history' = 'upcoming';
  isLoading = true;
  errorMessage = '';

  private toast = inject(ToastService);

  // 📄 Pagination Configuration
  currentPage = 1;
  itemsPerPage = 5;
  totalPages = 1;
  totalItems = 0;
  pagesArray: number[] = [];

  // 🎨 Evaluation Modal State
  isEvaluationModalOpen = false;
  selectedInterview: MyInterviewDto | null = null;
  isSubmitting = false;
  isReadOnly = false; // 🔒 Read-only mode for viewing history

  // ... (evaluationForm remains the same)


  // 📝 Evaluation Form Model
  evaluationForm = {
    technicalSkills: 0,    // 1-5 stars
    communication: 0,      // 1-5 stars
    attitude: 0,          // 1-5 stars
    experience: 0,        // 1-5 stars
    overallScore: 0,      // 0-100
    comment: '',
    decision: '' as 'Passed' | 'Failed' | 'Consider' | '',
    submittedByName: '' as string | undefined,
    isBelated: false
  };


  // 🚀 Performance: Cache interview states
  private interviewStateCache = new Map<string, InterviewState>();

  private platformId = inject(PLATFORM_ID); // ⚡ SSR Fix

  protected Math = Math; // 🔢 Expose Math for HTML template

  constructor(
    private interviewService: InterviewService,
    private evaluationService: EvaluationService,
    public authService: AuthService,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    // ⚡ SSR Fix: Only load data on client side
    if (isPlatformBrowser(this.platformId)) {
      this.loadMyInterviews();
    }
  }

  /**
   * Gọi API để lấy danh sách lịch phỏng vấn
   */
  loadMyInterviews(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.markForCheck(); // ⚡ Trigger check for loading state

    this.interviewService.getMyInterviews().subscribe({
      next: (response) => {
        if (response.success) {
          this.allInterviews = response.data;
          this.isLoading = false; // ⚡ Set before applyFilter

          //  DEBUG: Log status of loaded interviews
          console.log(' Loaded interviews (Raw):', this.allInterviews.map(i => ({ id: i.interviewId, status: i.status })));

          this.applyFilter();
        } else {
          this.errorMessage = response.message || 'Không thể tải lịch phỏng vấn';
          this.isLoading = false;
        }
        this.cdr.markForCheck(); // ⚡ Mark for check before detect
        this.cdr.detectChanges(); // ⚡ Force update
      },
      error: (error) => {
        console.error(' Error loading interviews:', error);
        this.errorMessage = 'Có lỗi xảy ra khi tải lịch phỏng vấn';
        this.isLoading = false;
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      }
    });
  }

  /**
   * Thay đổi filter (Sắp tới / Lịch sử)
   */
  setFilter(type: 'upcoming' | 'history'): void {
    this.filterType = type;
    this.currentPage = 1; // Reset to page 1
    this.applyFilter();
    this.cdr.detectChanges(); // ⚡ Force UI update immediately
  }

  /**
   * Áp dụng filter vào danh sách
   */
  applyFilter(): void {
    // Clear cache when filtering
    this.interviewStateCache.clear();

    if (this.filterType === 'upcoming') {
      // Lọc: Chưa có feedback (bao gồm cả quá hạn, sắp tới, hôm nay)
      this.interviews = this.allInterviews.filter(interview => {
        const state = this.getInterviewState(interview);
        return state.statusType !== 'completed';
      });
    } else {
      // Lọc: Đã hoàn thành (có feedback)
      this.interviews = this.allInterviews.filter(interview => {
        const state = this.getInterviewState(interview);
        return state.statusType === 'completed';
      });
    }

    // 📄 Update Pagination
    this.totalItems = this.interviews.length;
    this.totalPages = Math.ceil(this.totalItems / this.itemsPerPage);
    if (this.totalPages === 0) this.totalPages = 1;
    this.pagesArray = Array.from({ length: this.totalPages }, (_, i) => i + 1);

    this.updatePagination();
    this.cdr.detectChanges(); // ⚡ Force UI update after filtering
  }

  /**
   * 📄 Cập nhật danh sách hiển thị theo phân trang
   */
  updatePagination(): void {
    const startIndex = (this.currentPage - 1) * this.itemsPerPage;
    const endIndex = startIndex + this.itemsPerPage;
    this.paginatedInterviews = this.interviews.slice(startIndex, endIndex);
    this.cdr.markForCheck();
    this.cdr.detectChanges(); // ⚡ Force UI update
  }

  /**
   * 📄 Chuyển trang
   */
  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.updatePagination();
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.updatePagination();
    }
  }

  prevPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.updatePagination();
    }
  }

  /**
   * 🎯 CORE LOGIC: Tính toán trạng thái UI cho từng interview
   * 🚀 OPTIMIZED: Caches results to avoid repeated calculations
   * 
   * Business Rules (Updated - Calendar Date based):
   * 1. COMPLETED: hasFeedback = true → Đã xong
   * 2. OVERDUE: scheduledDate < today (khác ngày) VÀ hasFeedback = false → Quá hạn
   * 3. READY/TODAY: scheduledDate === today (cùng ngày) → Đang diễn ra
   * 4. UPCOMING: scheduledDate > today (tương lai) → Sắp tới
   */
  getInterviewState(interview: MyInterviewDto): InterviewState {
    // Check cache first
    const cached = this.interviewStateCache.get(interview.interviewId);
    if (cached) {
      return cached;
    }
    const now = new Date();
    const scheduledTime = new Date(interview.interviewTime);

    // 🛡️ Robust Check: Normalize status to lower case for comparison
    const normalizedStatus = (interview.status || '').toLowerCase();
    const hasFeedback = normalizedStatus === 'completed';

    // Helper: So sánh 2 dates theo Calendar Date (bỏ qua giờ phút)
    const isSameDay = (d1: Date, d2: Date): boolean => {
      return d1.getFullYear() === d2.getFullYear() &&
        d1.getMonth() === d2.getMonth() &&
        d1.getDate() === d2.getDate();
    };

    const isBeforeToday = (d: Date): boolean => {
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      const compareDate = new Date(d);
      compareDate.setHours(0, 0, 0, 0);
      return compareDate < today;
    };

    const isAfterToday = (d: Date): boolean => {
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      const compareDate = new Date(d);
      compareDate.setHours(0, 0, 0, 0);
      return compareDate > today;
    };

    // Case 1:  COMPLETED - Đã có feedback
    if (hasFeedback) {
      const state: InterviewState = {
        badgeClass: 'px-3 py-1 rounded-full text-xs font-semibold bg-green-100 text-green-700 border border-green-200',
        badgeLabel: 'Hoàn thành',
        buttonText: 'Xem lại',
        buttonClass: 'px-4 py-2 bg-white text-gray-700 border border-gray-300 rounded-lg text-sm font-medium hover:bg-gray-50 transition-colors shadow-sm',
        isButtonDisabled: false, //  Enable button for viewing
        isOverdue: false,
        statusType: 'completed'
      };
      this.interviewStateCache.set(interview.interviewId, state);
      return state;
    }

    // Case 2:  OVERDUE - Quá hạn (Ngày phỏng vấn < Ngày hôm nay)
    // Lưu ý: Nếu cùng ngày hôm nay nhưng giờ đã qua → vẫn là "Hôm nay", CHƯA phải quá hạn
    if (isBeforeToday(scheduledTime)) {
      const state: InterviewState = {
        badgeClass: 'px-3 py-1 rounded-full text-xs font-semibold bg-red-100 text-red-700 border border-red-200 animate-pulse',
        badgeLabel: 'Quá hạn',
        buttonText: 'Chấm bù',
        buttonClass: 'px-4 py-2 bg-red-600 text-white rounded-lg text-sm font-semibold hover:bg-red-700 transition-colors shadow-sm',
        isButtonDisabled: false,
        isOverdue: true,
        statusType: 'overdue'
      };
      this.interviewStateCache.set(interview.interviewId, state);
      return state;
    }

    // Case 3: 🔵 READY/TODAY - Hôm nay (Ngày phỏng vấn === Ngày hôm nay)
    if (isSameDay(scheduledTime, now)) {
      const state: InterviewState = {
        badgeClass: 'px-3 py-1 rounded-full text-xs font-semibold bg-blue-100 text-blue-700 border border-blue-200',
        badgeLabel: 'Hôm nay',
        buttonText: 'Chấm điểm',
        buttonClass: 'px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-semibold hover:bg-blue-700 transition-colors shadow-sm',
        isButtonDisabled: false,
        isOverdue: false,
        statusType: 'ready'
      };
      this.interviewStateCache.set(interview.interviewId, state);
      return state;
    }

    // Case 4: 🟡 UPCOMING - Sắp tới (Ngày phỏng vấn > Ngày hôm nay)
    if (isAfterToday(scheduledTime)) {
      const state: InterviewState = {
        badgeClass: 'px-3 py-1 rounded-full text-xs font-semibold bg-yellow-100 text-yellow-700 border border-yellow-200',
        badgeLabel: 'Sắp tới',
        buttonText: 'Chưa đến giờ',
        buttonClass: 'px-4 py-2 bg-gray-300 text-gray-500 rounded-lg text-sm font-medium cursor-not-allowed',
        isButtonDisabled: true,
        isOverdue: false,
        statusType: 'upcoming'
      };
      this.interviewStateCache.set(interview.interviewId, state);
      return state;
    }

    // Fallback (không nên xảy ra)
    const fallbackState: InterviewState = {
      badgeClass: 'px-3 py-1 rounded-full text-xs font-semibold bg-gray-100 text-gray-700 border border-gray-200',
      badgeLabel: 'Không xác định',
      buttonText: 'N/A',
      buttonClass: 'px-4 py-2 bg-gray-300 text-gray-500 rounded-lg text-sm font-medium cursor-not-allowed',
      isButtonDisabled: true,
      isOverdue: false,
      statusType: 'upcoming'
    };

    // Cache before returning
    this.interviewStateCache.set(interview.interviewId, fallbackState);
    return fallbackState;
  }

  /**
   * Kiểm tra có thể chấm điểm hay không
   * Legacy method - giữ lại để tương thích
   */
  canEdit(interview: MyInterviewDto): boolean {
    const state = this.getInterviewState(interview);
    return !state.isButtonDisabled;
  }

  /**
   * Generate avatar color class for candidate initials
   */
  getAvatarColor(index: number): string {
    const colors = [
      'bg-blue-100 text-blue-700 border-blue-200',
      'bg-green-100 text-green-700 border-green-200',
      'bg-purple-100 text-purple-700 border-purple-200',
      'bg-orange-100 text-orange-700 border-orange-200',
      'bg-pink-100 text-pink-700 border-pink-200'
    ];
    return colors[index % colors.length];
  }

  /**
   * Get candidate initials from name
   */
  getInitials(name: string): string {
    if (!name) return 'N/A';
    const parts = name.split(' ');
    if (parts.length >= 2) {
      return parts[0][0] + parts[parts.length - 1][0];
    }
    return name.substring(0, 2).toUpperCase();
  }

  /**
   * TrackBy function for ngFor optimization
   */
  trackByInterviewId(index: number, interview: MyInterviewDto): string {
    return interview.interviewId;
  }

  // ==================== EVALUATION MODAL METHODS ====================

  /**
   * Handle interview action button click
   * Điều hướng đúng method dựa trên trạng thái interview
   */
  handleInterviewAction(interview: MyInterviewDto): void {
    const state = this.getInterviewState(interview);

    // Debug log
    console.log(' handleInterviewAction called', {
      interviewId: interview.interviewId,
      statusType: state.statusType,
      isDisabled: state.isButtonDisabled
    });

    // Nếu button bị disabled thì không làm gì
    if (state.isButtonDisabled) {
      console.log(' Button is disabled, ignoring click');
      return;
    }

    // Nếu là completed -> Xem lại
    if (state.statusType === 'completed') {
      console.log(' Opening view evaluation modal');
      this.viewEvaluation(interview);
    } else {
      // Các trường hợp khác -> Chấm điểm
      console.log(' Opening evaluation modal for scoring');
      this.openEvaluationModal(interview);
    }
  }

  /**
   * Open evaluation modal
   */
  openEvaluationModal(interview: MyInterviewDto): void {
    this.selectedInterview = interview;
    this.isEvaluationModalOpen = true;
    this.resetEvaluationForm();
    this.cdr.detectChanges();
  }

  /**
   * Close evaluation modal
   */
  closeEvaluationModal(): void {
    this.isEvaluationModalOpen = false;
    this.selectedInterview = null;
    this.resetEvaluationForm();
  }

  /**
   * Reset evaluation form
   */
  resetEvaluationForm(): void {
    this.isReadOnly = false; // Reset read-only state
    this.evaluationForm = {
      technicalSkills: 0,
      communication: 0,
      attitude: 0,
      experience: 0,
      overallScore: 0,
      comment: '',
      decision: '',
      submittedByName: undefined,
      isBelated: false
    };
  }

  /**
   * Kiểm tra xem đang thực hiện đánh giá hộ người khác hay không
   */
  isEvaluationOnBehalf(): boolean {
    if (!this.selectedInterview || !this.selectedInterview.interviewerId) return false;
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser || !currentUser.userId) return false;
    return currentUser.userId.toLowerCase() !== this.selectedInterview.interviewerId.toLowerCase();
  }

  /**
   * Calculate overall score from star ratings
   */
  calculateOverallScore(): void {
    const sum = this.evaluationForm.technicalSkills +
      this.evaluationForm.communication +
      this.evaluationForm.attitude +
      this.evaluationForm.experience;

    // Convert 20-point scale (4 criteria × 5 stars) to 100-point scale
    this.evaluationForm.overallScore = Math.round((sum / 20) * 100);
  }

  /**
   * Set star rating for a criterion
   */
  setRating(criterion: keyof typeof this.evaluationForm, value: number): void {
    if (criterion === 'technicalSkills' || criterion === 'communication' ||
      criterion === 'attitude' || criterion === 'experience') {
      this.evaluationForm[criterion] = value;
      this.calculateOverallScore();
    }
  }

  /**
   * View evaluation history (Read-only)
   */
  viewEvaluation(interview: MyInterviewDto): void {
    if (!interview.interviewId) return;

    //  Không set this.isLoading = true vì nó sẽ che toàn bộ trang
    this.evaluationService.getEvaluation(interview.interviewId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          const data = response.data;

          // Open Modal
          this.selectedInterview = interview;
          this.isEvaluationModalOpen = true;
          this.isReadOnly = true; // 🔒 Set Read-only mode

          // ⚡ Force modal to open immediately
          this.cdr.detectChanges();

          // Parse JSON details
          let details: any[] = [];
          try {
            console.log('📥 Raw Details JSON:', data.details); //  DEBUG LOG
            details = data.details ? JSON.parse(data.details) : [];
            console.log(' Parsed Details Array:', details); //  DEBUG LOG
          } catch (e) {
            console.error(' Error parsing evaluation details:', e);
          }

          // Populate form (Handle both Title Case and Camel Case just in case)
          // details structure: { criterion: "...", score: ... }
          const getScore = (name: string) => {
            const item = details.find((d: any) =>
              (d.Criterion || d.criterion) === name ||
              (d.Criterion || d.criterion)?.toLowerCase() === name.toLowerCase()
            );
            return item ? (item.Score || item.score || 0) : 0;
          };

          this.evaluationForm = {
            technicalSkills: getScore('Kỹ năng chuyên môn'),
            communication: getScore('Kỹ năng giao tiếp'),
            attitude: getScore('Thái độ & Nhiệt tình'),
            experience: getScore('Kinh nghiệm'),
            overallScore: data.score,
            comment: data.comment || '',
            decision: data.result as 'Passed' | 'Failed' | 'Consider',
            submittedByName: data.submittedByName,
            isBelated: data.isBelated || false
          };

          console.log('📝 Populated Form:', this.evaluationForm); //  DEBUG LOG

          this.cdr.markForCheck();
        } else {
          // Show error if not found
          this.toast.error('Lỗi', 'Không tìm thấy chi tiết đánh giá');
        }
      },
      error: (err) => {
        console.error(' Error fetching evaluation:', err);
        this.toast.error('Lỗi', 'Có lỗi xảy ra khi tải chi tiết đánh giá');
      }
    });
  }

  /**
   * Check if evaluation form is valid
   */
  isEvaluationFormValid(): boolean {
    return this.evaluationForm.technicalSkills > 0 &&
      this.evaluationForm.communication > 0 &&
      this.evaluationForm.attitude > 0 &&
      this.evaluationForm.experience > 0 &&
      this.evaluationForm.decision !== '' &&
      this.evaluationForm.comment.trim().length > 0;
  }

  /**
   * Validate form before submit
   */
  validateForm(): boolean {
    const f = this.evaluationForm;
    return f.technicalSkills > 0 &&
      f.communication > 0 &&
      f.attitude > 0 &&
      f.experience > 0 &&
      f.decision !== '' &&
      f.comment.trim().length > 0;
  }

  /**
   * Submit evaluation
   */
  async submitEvaluation(): Promise<void> {
    if (!this.isEvaluationFormValid() || !this.selectedInterview) {
      console.warn('Form validation failed or no interview selected');
      return;
    }

    this.isSubmitting = true;

    try {
      // Get current user ID from auth service
      const currentUser = this.authService.getCurrentUser();
      if (!currentUser || !currentUser.userId) {
        throw new Error('User not authenticated');
      }

      // Prepare evaluation details as JSON
      const details: EvaluationDetail[] = [
        { criterion: 'Kỹ năng chuyên môn', score: this.evaluationForm.technicalSkills, maxScore: 5 },
        { criterion: 'Kỹ năng giao tiếp', score: this.evaluationForm.communication, maxScore: 5 },
        { criterion: 'Thái độ & Nhiệt tình', score: this.evaluationForm.attitude, maxScore: 5 },
        { criterion: 'Kinh nghiệm', score: this.evaluationForm.experience, maxScore: 5 }
      ];

      const dto: EvaluationSubmitDto = {
        interviewId: this.selectedInterview.interviewId,
        interviewerId: this.selectedInterview.interviewerId, // Luôn gửi đúng thẻ ID của người được phân công ban đầu
        score: this.evaluationForm.overallScore,
        comment: this.evaluationForm.comment.trim(),
        result: this.evaluationForm.decision as 'Passed' | 'Failed' | 'Consider',
        details: JSON.stringify(details),
        submittedById: currentUser.userId
      };


      await this.evaluationService.submitEvaluation(dto).toPromise();

      if (this.selectedInterview) {
        const interview = this.allInterviews.find(i => i.interviewId === this.selectedInterview!.interviewId);
        if (interview) {
          interview.status = 'completed';
        }

        this.interviewStateCache.delete(this.selectedInterview.interviewId);
      }

      // Close modal
      this.closeEvaluationModal();

      // Re-apply filter to move item to 'History' or update status UI
      this.applyFilter();

      // Sync with server in background
      this.loadMyInterviews();

      // TODO: Show success notification
      this.toast.success('Đã gửi đánh giá', 'Đánh giá phỏng vấn đã được lưu thành công!');
    } catch (error) {
      console.error(' Error submitting evaluation:', error);
      console.dir(error);
      this.toast.error('Lỗi lưu đánh giá', 'Có lỗi xảy ra khi lưu đánh giá. Vui lòng thử lại!');
    } finally {
      this.isSubmitting = false;
      this.cdr.detectChanges();
    }
  }
}

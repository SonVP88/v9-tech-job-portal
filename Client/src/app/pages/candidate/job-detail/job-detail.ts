import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { CandidateHeaderComponent } from '../../../components/shared/candidate-header/candidate-header';
import { CandidateFooter } from '../../../components/shared/candidate-footer/candidate-footer';
import { SavedJobService } from '../../../services/saved-job.service';
import { ToastService } from '../../../services/toast.service';

export interface JobDetailDto {
  jobId: string;
  title: string;
  companyName: string;
  salaryMin: number | null;
  salaryMax: number | null;
  location: string | null;
  employmentType: string | null;
  experienceLevel: string | null;
  seniorityLevel: string | null;
  deadline: string | null;
  createdDate: string;
  skills: string[];
  description: string | null;
  requirements: string | null;
  benefits: string | null;
  contactEmail: string | null;
  numberOfPositions: number | null;
}

export interface AssessmentDto {
  assessmentId: string;
  title: string;
  description: string | null;
  duration: number; // in minutes
}

export interface CodeChallengeDto {
  challengeId: string;
  title: string;
  difficulty: string;
  description: string | null;
}

@Component({
  selector: 'app-job-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, CandidateHeaderComponent, CandidateFooter],
  templateUrl: './job-detail.html',
  styleUrl: './job-detail.scss',
})
export class JobDetail implements OnInit {
  job: JobDetailDto | null = null;
  loading = true;
  error: string | null = null;
  
  // Assessment and Code Challenge properties
  assessments: AssessmentDto[] = [];
  codeChallenges: CodeChallengeDto[] = [];
  loadingAssessments = false;
  loadingChallenges = false;

  applyForm!: FormGroup;
  selectedFile: File | null = null;
  selectedFileName: string = '';
  isSubmitting = false;
  submitSuccess = false;
  submitError: string | null = null;
  isDragging = false;
  isModalOpen = false;

  isSaved = false;
  isTogglingSave = false;

  private apiUrl = '/api';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient,
    private cdr: ChangeDetectorRef,
    private fb: FormBuilder,
    private savedJobService: SavedJobService,
    private toast: ToastService
  ) { }

  ngOnInit(): void {
    // Regex for standard email format
    const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    // Regex for Vietnamese phone numbers (10 digits starting with 0)
    const phoneRegex = /(84|0[3|5|7|8|9])+([0-9]{8})\b/;

    this.applyForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.pattern(emailRegex), Validators.maxLength(100)]],
      phone: ['', [Validators.required, Validators.pattern(phoneRegex)]],
      introduction: ['', [Validators.maxLength(2000)]]
    });

    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      this.error = 'Invalid job ID';
      this.loading = false;
      return;
    }

    this.loadJobDetail(id);
  }

  /**
   * Gọi API lấy chi tiết job
   */
  loadJobDetail(id: string): void {
    this.loading = true;
    this.error = null;

    this.http.get<JobDetailDto>(`${this.apiUrl}/jobs/${id}`)
      .subscribe({
        next: (job) => {
          this.job = job;
          this.loading = false;

          this.checkIfSaved(job.jobId);
          // Fetch assessments and code challenges for this job
          this.loadAssessmentsForJob(job.jobId);
          this.loadCodeChallengesForJob(job.jobId);

          this.cdr.detectChanges();
          console.log('Loaded job detail:', job);
        },
        error: (err) => {
          console.error('Error loading job:', err);
          if (err.status === 404) {
            this.error = 'Job not found';
          } else {
            this.error = 'Failed to load job details';
          }
          this.loading = false;
          this.cdr.detectChanges();
        }
      });
  }

  /**
   * Load assessments for the job
   */
  private loadAssessmentsForJob(jobId: string): void {
    this.loadingAssessments = true;
    
    this.http.get<any>(`${this.apiUrl}/assessments/jobs/${jobId}/assessments`)
      .subscribe({
        next: (response) => {
          this.assessments = response.data || [];
          this.loadingAssessments = false;
          this.cdr.detectChanges();
          console.log('Loaded assessments:', this.assessments);
        },
        error: (err) => {
          console.error('Error loading assessments:', err);
          this.assessments = [];
          this.loadingAssessments = false;
          this.cdr.detectChanges();
        }
      });
  }

  /**
   * Load code challenges for the job
   */
  private loadCodeChallengesForJob(jobId: string): void {
    this.loadingChallenges = true;
    
    this.http.get<any>(`${this.apiUrl}/v2/aicode-assessment/jobs/${jobId}/challenges`)
      .subscribe({
        next: (response) => {
          this.codeChallenges = response.data || [];
          this.loadingChallenges = false;
          this.cdr.detectChanges();
          console.log('Loaded code challenges:', this.codeChallenges);
        },
        error: (err) => {
          console.error('Error loading code challenges:', err);
          this.codeChallenges = [];
          this.loadingChallenges = false;
          this.cdr.detectChanges();
        }
      });
  }

  /**
   * Handle file selection
   */
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      this.validateAndSetFile(file);
    }
  }

  /**
   * Handle drag over
   */
  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = true;
  }

  /**
   * Handle drag leave
   */
  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;
  }

  /**
   * Handle file drop
   */
  onFileDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;

    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      const file = event.dataTransfer.files[0];
      this.validateAndSetFile(file);
    }
  }

  /**
   * Check if job is saved
   */
  private checkIfSaved(jobId: string): void {
    const token = localStorage.getItem('authToken');
    if (!token) return;

    this.savedJobService.checkSaved(jobId).subscribe({
      next: (response) => {
        this.isSaved = response.saved;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error checking saved status:', err);
      }
    });
  }

  /**
   * Toggle save job status
   */
  toggleSaveJob(): void {
    const token = localStorage.getItem('authToken');
    if (!token) {
      this.toast.warning('Yêu cầu đăng nhập', 'Bạn cần đăng nhập để lưu công việc này!');
      this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }

    if (!this.job || this.isTogglingSave) return;

    this.isTogglingSave = true;
    this.savedJobService.toggleSave(this.job.jobId).subscribe({
      next: (response) => {
        this.isSaved = response.saved;
        this.isTogglingSave = false;
        if (this.isSaved) {
          this.toast.success('Đã lưu công việc', 'Sẽ dễ dàng tìm lại công việc này sau.');
        } else {
          this.toast.success('Bỏ lưu thành công', 'Đã bỏ lưu công việc.');
        }
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error toggling saved status:', err);
        this.toast.error('Có lỗi xảy ra', 'Không thể thay đổi trạng thái lưu công việc. Vui lòng thử lại!');
        this.isTogglingSave = false;
      }
    });
  }

  /**
   * Validate and set file
   */
  private validateAndSetFile(file: File): void {
    const allowedExtensions = ['.pdf'];
    const fileExtension = '.' + file.name.split('.').pop()?.toLowerCase();

    if (!allowedExtensions.includes(fileExtension)) {
      this.submitError = 'Chỉ chấp nhận file PDF';
      this.selectedFile = null;
      this.selectedFileName = '';
      this.toast.error('File không hợp lệ', 'Chỉ chấp nhận file PDF. Vui lòng chọn file PDF khác!');
      return;
    }

    const maxSize = 5 * 1024 * 1024; // 5MB
    if (file.size > maxSize) {
      this.submitError = 'File không được vượt quá 5MB';
      this.selectedFile = null;
      this.selectedFileName = '';
      this.toast.error('File quá lớn', 'File không được vượt quá 5MB. Vui lòng chọn file nhỏ hơn!');
      return;
    }

    this.selectedFile = file;
    this.selectedFileName = file.name;
    this.submitError = null;
    this.toast.success('File được chấp nhận', `${file.name} đã được chọn. Sẵn sàng gửi hồ sơ!`);
  }

  /**
   * Remove selected file
   */
  removeFile(): void {
    this.selectedFile = null;
    this.selectedFileName = '';
  }

  /**
   * Submit application
   */
  onSubmitApply(): void {

    this.submitSuccess = false;
    this.submitError = null;


    if (this.applyForm.invalid) {
      Object.keys(this.applyForm.controls).forEach(key => {
        this.applyForm.get(key)?.markAsTouched();
      });
      this.submitError = 'Vui lòng điền đầy đủ thông tin';
      return;
    }

    if (!this.selectedFile) {
      this.submitError = 'Vui lòng chọn file CV';
      return;
    }

    if (!this.job) {
      this.submitError = 'Không tìm thấy thông tin công việc';
      return;
    }

    this.isSubmitting = true;

    const formData = new FormData();
    formData.append('jobId', this.job.jobId);
    formData.append('fullName', this.applyForm.get('fullName')?.value);
    formData.append('email', this.applyForm.get('email')?.value);
    formData.append('phone', this.applyForm.get('phone')?.value);
    formData.append('introduction', this.applyForm.get('introduction')?.value || '');
    formData.append('cvFile', this.selectedFile);

    this.http.post<any>(`${this.apiUrl}/applications/apply`, formData)
      .subscribe({
        next: (response) => {
          console.log('Application submitted successfully:', response);
          this.isSubmitting = false;
          this.submitSuccess = true;
          this.submitError = null;

          this.toast.success('Ứng tuyển thành công', 'Hồ sơ đã được gửi đến nhà tuyển dụng!');

          this.applyForm.reset();
          this.selectedFile = null;
          this.selectedFileName = '';
        },
        error: (err) => {
          console.error('Error submitting application:', err);
          this.isSubmitting = false;
          this.submitSuccess = false;

          // Handle specific error from backend
          const errorMessage = err.error?.message || 'Có lỗi xảy ra. Vui lòng thử lại sau.';
          
          // Check if it's a duplicate application error
          if (errorMessage.includes('đã nộp hồ sơ') || errorMessage.includes('đã apply')) {
            this.submitError = 'Đã gửi đơn cho công việc này rồi. Không thể gửi lại.';
          } else if (err.status === 400) {
            this.submitError = errorMessage;
          } else {
            this.submitError = 'Có lỗi xảy ra. Vui lòng thử lại sau.';
          }
          
          this.toast.error('Ứng tuyển thất bại', this.submitError || 'Có lỗi xảy ra. Vui lòng thử lại sau.');
        }
      });
  }

  /**
   * Format salary range
   */
  formatSalary(min: number | null, max: number | null): string {
    if (!min && !max) return 'Negotiable';

    const formatNumber = (num: number) => {
      if (num >= 1000000) {
        return `${(num / 1000000).toFixed(0)}M VNĐ`;
      }
      return `${num.toLocaleString('vi-VN')} VNĐ`;
    };

    if (min && max) {
      return `${formatNumber(min)} - ${formatNumber(max)}`;
    } else if (min) {
      return `From ${formatNumber(min)}`;
    } else if (max) {
      return `Up to ${formatNumber(max)}`;
    }

    return 'Negotiable';
  }

  /**
   * Format date
   */
  formatDate(dateString: string | null | undefined): string {
    if (!dateString) return 'N/A';

    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  getExperienceLabel(): string {
    if (this.job?.experienceLevel) return this.job.experienceLevel;

    const requirements = this.job?.requirements;
    if (!requirements) return 'Chưa cập nhật';

    const lines = requirements.split('\n').map(line => line.trim()).filter(Boolean);
    const expLine = lines.find(line => /^Kinh nghiệm\s*:/i.test(line));
    if (!expLine) return 'Chưa cập nhật';

    return expLine.replace(/^Kinh nghiệm\s*:/i, '').trim() || 'Chưa cập nhật';
  }

  getSeniorityLabel(): string {
    if (this.job?.seniorityLevel) return this.job.seniorityLevel;

    const title = this.job?.title;
    if (!title) return 'Chưa cập nhật';

    const normalized = title.toLowerCase();
    if (normalized.includes('intern') || normalized.includes('thực tập')) return 'Intern';
    if (normalized.includes('fresher')) return 'Fresher';
    if (normalized.includes('junior')) return 'Junior';
    if (normalized.includes('senior')) return 'Senior';
    if (normalized.includes('lead')) return 'Lead';
    if (normalized.includes('manager')) return 'Manager';
    if (normalized.includes('mid')) return 'Middle';

    return 'Chưa cập nhật';
  }

  getContactName(): string {
    return this.job?.companyName || 'Nhà tuyển dụng';
  }

  getWebsiteLabel(): string {
    const email = this.job?.contactEmail;
    if (!email || !email.includes('@')) return 'Chưa cập nhật';
    return email.split('@')[1];
  }

  getWebsiteUrl(): string | null {
    const email = this.job?.contactEmail;
    if (!email || !email.includes('@')) return null;
    const domain = email.split('@')[1];
    return `https://${domain}`;
  }

  getContactAvatar(): string {
    const name = this.getContactName();
    return `https://ui-avatars.com/api/?name=${encodeURIComponent(name)}&background=3B82F6&color=fff&bold=true`;
  }

  /**
   * Calculate days ago
   */
  getDaysAgo(dateString: string): string {
    const date = new Date(dateString);
    const now = new Date();
    const diffTime = now.getTime() - date.getTime();
    const diffDays = Math.floor(diffTime / (1000 * 60 * 60 * 24));

    if (diffDays === 0) return 'Today';
    if (diffDays === 1) return '1 day ago';
    if (diffDays < 7) return `${diffDays} days ago`;
    if (diffDays < 30) return `${Math.floor(diffDays / 7)} weeks ago`;
    return `${Math.floor(diffDays / 30)} months ago`;
  }

  /**
   * Navigate back
   */
  goBack(): void {
    this.router.navigate(['/candidate/home']);
  }

  /**
   * Open apply modal
   */
  openApplyModal(): void {
    this.isModalOpen = true;
    this.submitSuccess = false;
    this.submitError = null;

    // Auto-fill email từ tài khoản đang đăng nhập (nếu có)
    const token = localStorage.getItem('authToken');
    if (token) {
      try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        // Lấy email từ claim 'email' hoặc 'sub' hoặc 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'
        const emailFromToken =
          payload['email'] ||
          payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ||
          payload['unique_name'] ||
          null;
        if (emailFromToken) {
          this.applyForm.patchValue({ email: emailFromToken });
        }
      } catch (e) {
        // Bỏ qua nếu token không hợp lệ
      }
    }
  }

  /**
   * Helper method to check if a form field is invalid and touched
   */
  isFieldInvalid(fieldName: string): boolean {
    const field = this.applyForm.get(fieldName);
    return field ? (field.invalid && (field.dirty || field.touched)) : false;
  }

  /**
   * Close apply modal
   */
  closeModal(): void {
    this.isModalOpen = false;
    if (!this.submitSuccess) {
      this.applyForm.reset();
      this.selectedFile = null;
      this.selectedFileName = '';
      this.submitError = null;
    }
  }

  /**
   * Navigate to assessment
   */
  startAssessment(assessmentId: string): void {
    const token = localStorage.getItem('authToken');
    if (!token) {
      this.toast.warning('Yêu cầu đăng nhập', 'Bạn cần đăng nhập để làm bài kiểm tra!');
      this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }

    this.router.navigate(['/candidate/assessment', assessmentId]);
  }

  /**
   * Navigate to code interview
   */
  startCodeChallenge(challengeId: string): void {
    const token = localStorage.getItem('authToken');
    if (!token) {
      this.toast.warning('Yêu cầu đăng nhập', 'Bạn cần đăng nhập để làm bài code!');
      this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }

    this.router.navigate(['/candidate/code-interview', challengeId]);
  }
}

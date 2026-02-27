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
  deadline: string | null;
  createdDate: string;
  skills: string[];
  description: string | null;
  requirements: string | null;
  benefits: string | null;
  contactEmail: string | null;
  numberOfPositions: number | null;
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
    this.applyForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.maxLength(200)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(320)]],
      phone: ['', [Validators.required, Validators.maxLength(50)]],
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
      this.router.navigate(['/candidate/login']);
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
    const allowedExtensions = ['.pdf', '.doc', '.docx'];
    const fileExtension = '.' + file.name.split('.').pop()?.toLowerCase();

    if (!allowedExtensions.includes(fileExtension)) {
      this.submitError = 'Chỉ chấp nhận file PDF, DOC hoặc DOCX';
      this.selectedFile = null;
      this.selectedFileName = '';
      return;
    }

    const maxSize = 5 * 1024 * 1024; // 5MB
    if (file.size > maxSize) {
      this.submitError = 'File không được vượt quá 5MB';
      this.selectedFile = null;
      this.selectedFileName = '';
      return;
    }

    this.selectedFile = file;
    this.selectedFileName = file.name;
    this.submitError = null;
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

          if (err.status === 409) {
            this.submitError = 'Bạn đã nộp hồ sơ cho công việc này rồi';
          } else if (err.status === 400) {
            this.submitError = err.error?.message || 'Dữ liệu không hợp lệ';
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
}

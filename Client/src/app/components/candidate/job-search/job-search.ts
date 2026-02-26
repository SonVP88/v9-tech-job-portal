import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { PublicJobService, JobPublic, JobSearchFilter } from '../../../services/public-job.service';
import { MasterDataService, Province } from '../../../services/master-data.service';
import { AuthService } from '../../../services/auth.service';
import { Router } from '@angular/router';
import { CandidateHeaderComponent } from '../../shared/candidate-header/candidate-header';
import { SavedJobService } from '../../../services/saved-job.service';
import { ToastService } from '../../../services/toast.service';
import { BehaviorSubject } from 'rxjs';

@Component({
  selector: 'app-job-search',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, CandidateHeaderComponent],
  templateUrl: './job-search.html',
  styleUrls: ['./job-search.scss']
})
export class JobSearchComponent implements OnInit {
  jobs: JobPublic[] = [];
  loading = false;
  provinces: Province[] = [];

  // Phân trang
  currentPage = 1;
  pageSize = 9;

  // Trạng thái đăng nhập
  isLoggedIn = false;
  userFullName = '';
  userRole = '';

  private savedJobIdsSubject = new BehaviorSubject<Set<string>>(new Set<string>());
  savedJobIds$ = this.savedJobIdsSubject.asObservable();

  // Trạng thái bộ lọc
  filter: JobSearchFilter = {
    keyword: '',
    location: '',
    jobType: '',
    minSalary: undefined
  };

  constructor(
    private publicJobService: PublicJobService,
    private masterDataService: MasterDataService,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef,
    private savedJobService: SavedJobService,
    private toast: ToastService
  ) { }

  ngOnInit(): void {
    this.checkAuthStatus();
    this.loadProvinces();
    if (this.authService.isAuthenticated()) {
      this.loadSavedIds();
    }

    // Lấy dữ liệu từ Resolver
    this.route.data.subscribe(data => {
      console.log('📦 Resolver data received:', data);
      const jobs = data['jobs'];
      if (jobs) {
        this.jobs = jobs;
        this.loading = false;
        // Đặt lại phân trang nếu cần
        if (this.currentPage > this.totalPages) {
          this.currentPage = 1;
        }
      } else {
        // Tải lại qua API nếu resolver không trả về dữ liệu
        this.loadJobs();
      }
    });
  }

  checkAuthStatus(): void {
    this.isLoggedIn = this.authService.isAuthenticated();
    if (this.isLoggedIn) {
      const user = this.authService.getCurrentUser();
      if (user) {
        this.userFullName = user.name || 'User';
        this.userRole = user.role || '';
      }
    }
  }

  logout(): void {
    this.authService.logout();
    this.checkAuthStatus();
    this.router.navigate(['/login']);
  }

  private loadSavedIds(): void {
    this.savedJobService.getSavedJobIds().subscribe({
      next: (ids) => {
        this.savedJobIdsSubject.next(new Set(ids));
      },
      error: () => { }
    });
  }

  toggleSaveJob(jobId: string, event: Event): void {
    event.stopPropagation();
    if (!this.authService.isAuthenticated()) {
      this.toast.warning('Yêu cầu đăng nhập', 'Bạn cần đăng nhập để lưu công việc này!');
      // Chuyển hướng sang trang đăng nhập nếu cần
      return;
    }
    this.savedJobService.toggleSave(jobId).subscribe({
      next: (res) => {
        const current = this.savedJobIdsSubject.getValue();
        if (res.saved) {
          current.add(jobId);
          this.toast.success('Đã lưu công việc', 'Công việc đã được lưu vào danh sách.');
        } else {
          current.delete(jobId);
          this.toast.success('Bỏ lưu thành công', 'Đã bỏ lưu công việc này.');
        }
        this.savedJobIdsSubject.next(new Set(current)); // emit giá trị mới để async pipe tự nhận biết
      },
      error: (err) => {
        console.error('Toggle save error:', err);
        this.toast.error('Lỗi', 'Không thể thực hiện hành động này lúc này.');
      }
    });
  }

  loadProvinces(): void {
    this.masterDataService.getProvinces().subscribe({
      next: (data) => {
        this.provinces = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to load provinces', err)
    });
  }

  loadJobs(): void {
    this.loading = true;
    this.cdr.detectChanges();

    // Chuẩn hóa bộ lọc tìm kiếm
    const searchFilter: JobSearchFilter = {
      keyword: this.filter.keyword || '',
      location: this.filter.location || '',
      jobType: this.filter.jobType || '',
      minSalary: this.filter.minSalary
    };

    console.log(' Searching jobs with filter:', searchFilter);

    this.publicJobService.searchJobs(searchFilter).subscribe({
      next: (data) => {
        this.jobs = data || [];
        this.loading = false;

        // Trở về trang 1 nếu vị trí hiện tại vượt quá tổng số trang
        if (this.currentPage > this.totalPages) {
          this.currentPage = 1;
        }

        console.log(` Loaded ${this.jobs.length} jobs`);
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(' Failed to load jobs:', err);
        this.jobs = [];
        this.loading = false;
        this.cdr.detectChanges();
      }
    });
  }

  onSearch(): void {
    this.currentPage = 1;
    this.loadJobs();
  }

  get pagedJobs(): JobPublic[] {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    const endIndex = startIndex + this.pageSize;
    return this.jobs.slice(startIndex, endIndex);
  }

  get totalPages(): number {
    return Math.ceil(this.jobs.length / this.pageSize) || 1;
  }

  get pages(): number[] {
    const total = this.totalPages;
    if (total <= 7) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }
    const visiblePages = 5;
    let startPage = Math.max(1, this.currentPage - Math.floor(visiblePages / 2));
    let endPage = Math.min(total, startPage + visiblePages - 1);
    if (endPage - startPage + 1 < visiblePages) {
      startPage = Math.max(1, endPage - visiblePages + 1);
    }
    return Array.from({ length: (endPage - startPage) + 1 }, (_, i) => startPage + i);
  }

  changePage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  onKeywordChange(keyword: string): void {
    this.filter.keyword = keyword;
  }

  onJobTypeChange(type: string, event: any): void {
    const isChecked = event.target.checked;
    if (isChecked) {
      this.filter.jobType = type;
    } else {
      if (this.filter.jobType === type) {
        this.filter.jobType = '';
      }
    }
    this.onSearch(); // Tự động tìm kiếm ngay khi bộ lọc thay đổi
  }

  isJobTypeSelected(type: string): boolean {
    return this.filter.jobType === type;
  }

  formatSalary(min?: number, max?: number): string {
    if (!min && !max) return 'Thỏa thuận';
    if (min && !max) return `Từ ${min.toLocaleString()} VNĐ`;
    if (!min && max) return `Lên tới ${max.toLocaleString()}`;
    return `${min?.toLocaleString()} - ${max?.toLocaleString()}`;
  }
}

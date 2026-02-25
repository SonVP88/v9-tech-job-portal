import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { JobDto, JobService } from '../../../services/job.service';
import { AuthService } from '../../../services/auth.service';
import { MasterDataService, Province } from '../../../services/master-data.service';
import { CandidateHeaderComponent } from '../../../components/shared/candidate-header/candidate-header';
import { SavedJobService } from '../../../services/saved-job.service';
import { NgZone } from '@angular/core';

// Interface khớp với Backend JobHomeDto - Dùng cái này hoặc JobDto từ service
export interface JobHomeDto extends JobDto { }

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, CandidateHeaderComponent],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home implements OnInit {
  featuredJobs: JobHomeDto[] = [];
  loading = true;
  error: string | null = null;
  isLoggedIn = false;
  userFullName = '';
  userRole = '';

  // Địa chỉ
  provinces: Province[] = [];
  selectedLocation = '';

  // Search
  searchKeyword = '';

  // Stats
  stats = {
    jobCount: 0,
    applicationCount: 0,
    candidateCount: 0
  };

  // Saved Jobs
  savedJobIds = new Set<string>();

  private apiUrl = '/api';

  constructor(
    private jobService: JobService,
    private cdr: ChangeDetectorRef,
    private authService: AuthService,
    private masterDataService: MasterDataService,
    private savedJobService: SavedJobService,
    private ngZone: NgZone
  ) { }

  ngOnInit(): void {
    this.checkAuthStatus();
    this.loadLatestJobs();
    this.loadProvinces();
    this.loadSystemStats();
    if (this.authService.isAuthenticated()) {
      this.loadSavedIds();
    }
  }

  private loadSavedIds(): void {
    this.ngZone.runOutsideAngular(() => {
      this.savedJobService.getSavedJobIds().subscribe({
        next: (ids) => {
          this.ngZone.run(() => {
            this.savedJobIds = new Set(ids);
            this.cdr.detectChanges();
          });
        },
        error: () => { } // Silently fail
      });
    });
  }

  toggleSaveJob(jobId: string, event: Event): void {
    event.stopPropagation();
    if (!this.authService.isAuthenticated()) return;

    this.ngZone.runOutsideAngular(() => {
      this.savedJobService.toggleSave(jobId).subscribe({
        next: (res) => {
          this.ngZone.run(() => {
            if (res.saved) {
              this.savedJobIds.add(jobId);
            } else {
              this.savedJobIds.delete(jobId);
            }
            this.savedJobIds = new Set(this.savedJobIds); // trigger change detection
            this.cdr.detectChanges();
          });
        },
        error: (err) => console.error('Toggle save error:', err)
      });
    });
  }

  loadSystemStats(): void {
    this.jobService.getSystemStats().subscribe({
      next: (data) => {
        this.stats = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to load stats', err)
    });
  }

  /**
   * Load danh sách tỉnh/thành phố
   */
  loadProvinces(): void {
    this.masterDataService.getProvinces()
      .subscribe({
        next: (provinces) => {
          this.provinces = provinces;
          console.log('Loaded provinces:', provinces);
        },
        error: (err) => {
          console.error('Error loading provinces:', err);
        }
      });
  }

  /**
   * Kiểm tra xem user đã đăng nhập chưa
   */
  checkAuthStatus(): void {
    this.isLoggedIn = this.authService.isAuthenticated();

    if (this.isLoggedIn) {
      const user = this.authService.getCurrentUser();
      if (user) {
        this.userFullName = user.name || 'User';
        this.userRole = user.role || '';
        console.log('✅ User đã đăng nhập:', {
          name: this.userFullName,
          email: user.email,
          role: this.userRole
        });
      }
    } else {
      console.log('ℹ️ User chưa đăng nhập');
    }
  }

  /**
   * Gọi API lấy 6 job mới nhất
   */
  loadLatestJobs(): void {
    this.loading = true;
    this.error = null;

    // Sử dụng JobService để tận dụng cấu hình Proxy (tránh lỗi SSL self-signed)
    this.jobService.getLatestJobs(6)
      .subscribe({
        next: (jobs) => {
          this.featuredJobs = jobs;
          this.loading = false;
          this.cdr.detectChanges(); // Trigger change detection manually
          console.log('Loaded jobs:', jobs);
        },
        error: (err) => {
          console.error('Error loading jobs:', err);
          this.error = 'Không thể tải danh sách việc làm. Vui lòng thử lại sau.';
          this.loading = false;
          this.cdr.detectChanges(); // Trigger change detection manually
        }
      });
  }

  /**
   * Format salary range
   * VD: formatSalary(10000000, 20000000) => "10 - 20 Triệu"
   */
  formatSalary(min: number | null | undefined, max: number | null | undefined): string {
    if (!min && !max) return 'Thỏa thuận';

    const formatNumber = (num: number) => {
      if (num >= 1000000) {
        return `${(num / 1000000).toFixed(0)} Triệu`;
      }
      return `${num.toLocaleString('vi-VN')} VNĐ`;
    };

    if (min && max) {
      return `${formatNumber(min)} - ${formatNumber(max)}`;
    } else if (min) {
      return `Từ ${formatNumber(min)}`;
    } else if (max) {
      return `Lên đến ${formatNumber(max)}`;
    }

    return 'Thỏa thuận';
  }

  /**
   * Format deadline
   */
  formatDeadline(deadline: string | null): string {
    if (!deadline) return 'Không giới hạn';

    const date = new Date(deadline);
    const now = new Date();
    const diffTime = date.getTime() - now.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    if (diffDays < 0) return 'Đã hết hạn';
    if (diffDays === 0) return 'Hôm nay';
    if (diffDays === 1) return 'Ngày mai';
    if (diffDays <= 7) return `Còn ${diffDays} ngày`;

    return date.toLocaleDateString('vi-VN');
  }

  /**
   * Tìm kiếm jobs theo keyword và location
   */
  onSearch(): void {
    this.loading = true;
    this.error = null;

    // Trim để loại bỏ whitespace và check giá trị thực sự
    const trimmedKeyword = this.searchKeyword?.trim();
    const trimmedLocation = this.selectedLocation?.trim();

    // Chỉ gửi parameter nếu có giá trị (không rỗng)
    const keywordParam = trimmedKeyword && trimmedKeyword.length > 0 ? trimmedKeyword : undefined;
    const locationParam = trimmedLocation && trimmedLocation.length > 0 ? trimmedLocation : undefined;

    console.log('🔍 Search triggered - Raw values:', {
      searchKeyword: this.searchKeyword,
      selectedLocation: this.selectedLocation
    });
    console.log('🔍 Search params (after trim):', {
      keyword: keywordParam,
      location: locationParam
    });

    this.jobService.getLatestJobs(6, keywordParam, locationParam)
      .subscribe({
        next: (jobs) => {
          this.featuredJobs = jobs;
          this.loading = false;

          if (jobs.length === 0) {
            const searchInfo = [];
            if (keywordParam) searchInfo.push(`từ khóa "${keywordParam}"`);
            if (locationParam) searchInfo.push(`địa điểm "${locationParam}"`);

            this.error = `Không tìm thấy việc làm phù hợp với ${searchInfo.join(' và ')}.`;
            console.log('⚠️ No results found');
          } else {
            this.error = null;
            console.log(`✅ Found ${jobs.length} jobs`);
          }

          this.cdr.detectChanges();
          console.log(`✅ Search completed:`, jobs);
        },
        error: (err) => {
          console.error('❌ Search API error:', err);
          this.error = 'Lỗi khi tìm kiếm. Vui lòng thử lại.';
          this.featuredJobs = []; // Clear danh sách khi có lỗi API
          this.loading = false;
          this.cdr.detectChanges();
        }
      });
  }

  /**
   * Clear search filters và load lại tất cả jobs
   */
  clearSearch(): void {
    this.searchKeyword = '';
    this.selectedLocation = '';
    this.error = null;
    this.loadLatestJobs();
    console.log('🔄 Search filters cleared, loading all jobs...');
  }

  /**
   * Đăng xuất
   */
  logout(): void {
    this.authService.logout();
  }
}

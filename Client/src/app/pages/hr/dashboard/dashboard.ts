import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, OnDestroy } from '@angular/core';
import { Router, RouterLink, NavigationEnd } from '@angular/router';
import { JobService, JobDto } from '../../../services/job.service';
import { DashboardService, DashboardSummaryDto, DashboardActivityDto, DashboardCandidateDto, WeeklyActivityDto, SlaDashboardDto } from '../../../services/dashboard.service';
import { filter } from 'rxjs/operators';
import { Subscription } from 'rxjs';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, Chart, registerables } from 'chart.js';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, BaseChartDirective],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit, OnDestroy {
  private routerSubscription?: Subscription;
  private isDestroyed = false;
  private slaRequestVersion = 0;
  private slaRetryTimer: ReturnType<typeof setTimeout> | null = null;
  private slaTimeoutTimer: ReturnType<typeof setTimeout> | null = null;

  // Tổng quan dashboard
  summary: DashboardSummaryDto = {
    totalCandidates: 0,
    openPositions: 0,
    interviewsToday: 0,
    newApplications: 0,
    totalCandidatesGrowth: 0,
    openPositionsGrowth: 0,
    interviewsTodayGrowth: 0,
    newApplicationsGrowth: 0
  };
  isLoadingSummary = false;

  // Hoạt động gần đây
  activities: DashboardActivityDto[] = [];
  isLoadingActivities = false;

  // Ứng viên mới nhất
  candidates: DashboardCandidateDto[] = [];
  isLoadingCandidates = false;

  // Biểu đồ hoạt động theo tuần
  public weeklyChartData: ChartData<'bar'> = {
    labels: [],
    datasets: [
      {
        data: [],
        label: 'Ứng tuyển',
        backgroundColor: 'rgba(191, 219, 254, 0.8)', // blue-200
        borderColor: 'rgba(191, 219, 254, 1)',
        borderWidth: 1
      },
      {
        data: [],
        label: 'Đã tuyển',
        backgroundColor: 'rgba(59, 130, 246, 0.8)', // primary blue
        borderColor: 'rgba(59, 130, 246, 1)',
        borderWidth: 1
      }
    ]
  };

  public weeklyChartOptions: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: {
        enabled: true,
        backgroundColor: 'rgba(0, 0, 0, 0.8)',
        titleColor: '#fff',
        bodyColor: '#fff',
        borderColor: '#ddd',
        borderWidth: 1
      }
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: { stepSize: 1 },
        grid: {
          display: true,
          color: 'rgba(0, 0, 0, 0.05)'
        }
      },
      x: {
        grid: {
          display: false
        }
      }
    }
  };

  public weeklyChartType = 'bar' as const;
  isLoadingChart = false;

  // Điểm nghẽn SLA
  slaDashboard: SlaDashboardDto = {
    totalTrackedApplications: 0,
    onTrackApplications: 0,
    overdueApplications: 0,
    warningApplications: 0,
    severeOverdueApplications: 0,
    complianceRate: 0,
    slaHealthScore: 0,
    recruiters: [],
    stages: [],
    topStuckApplications: []
  };
  isLoadingSla = false;
  slaOnlyMy = false;

  // SLA Alerts
  slaAlerts: DashboardActivityDto[] = [];
  isLoadingSlaAlerts = false;

  constructor(
    private jobService: JobService,
    private dashboardService: DashboardService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {
    // Đăng ký các thành phần của Chart.js
    Chart.register(...registerables);
  }

  ngOnInit(): void {
    this.loadAllDashboardData();

    this.jobService.refreshJobs();

    this.routerSubscription = this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: any) => {
        console.log(' Phát hiện điều hướng:', event.url);
        if (event.url === '/hr/dashboard' || event.url === '/hr') {
          console.log(' Tải lại dữ liệu dashboard...');
          this.loadAllDashboardData();
        }
      });
  }

  ngOnDestroy(): void {
    this.isDestroyed = true;
    this.routerSubscription?.unsubscribe();
    this.clearSlaTimers();
  }

  /**
  * Tải toàn bộ dữ liệu dashboard
   */
  loadAllDashboardData(): void {
    this.loadSummary();
    this.loadRecentActivity();
    this.loadLatestCandidates();
    this.loadWeeklyActivity();
    this.loadSlaBottleneck();
    this.loadSlaAlerts();
  }

  /**
  * Tải dữ liệu tổng quan
   */
  loadSummary(): void {
    this.isLoadingSummary = true;

    this.dashboardService.getSummary().subscribe({
      next: (data) => {
        console.log(' Đã tải tổng quan:', data);
        this.summary = data;
        this.isLoadingSummary = false;
        if (!this.isDestroyed) {
          this.cdr.detectChanges();
        }
      },
      error: (error) => {
        console.error(' Lỗi tải tổng quan:', error);
        this.isLoadingSummary = false;
        if (!this.isDestroyed) {
          this.cdr.detectChanges();
        }
      }
    });
  }

  /**
  * Tải hoạt động gần đây
   */
  loadRecentActivity(): void {
    this.isLoadingActivities = true;

    this.dashboardService.getRecentActivity(10).subscribe({
      next: (data) => {
        console.log(' Đã tải hoạt động:', data);
        this.activities = data;
        this.isLoadingActivities = false;
        if (!this.isDestroyed) {
          this.cdr.detectChanges();
        }
      },
      error: (error) => {
        console.error(' Lỗi tải hoạt động:', error);
        this.isLoadingActivities = false;
        if (!this.isDestroyed) {
          this.cdr.detectChanges();
        }
      }
    });
  }

  /**
  * Tải ứng viên mới nhất
   */
  loadLatestCandidates(): void {
    this.isLoadingCandidates = true;

    this.dashboardService.getLatestCandidates(2).subscribe({
      next: (data) => {
        console.log(' Đã tải danh sách ứng viên:', data);
        this.candidates = data;
        this.isLoadingCandidates = false;
        if (!this.isDestroyed) {
          this.cdr.detectChanges();
        }
      },
      error: (error) => {
        console.error(' Lỗi tải ứng viên:', error);
        this.isLoadingCandidates = false;
        if (!this.isDestroyed) {
          this.cdr.detectChanges();
        }
      }
    });
  }

  /**
  * Tải dữ liệu biểu đồ hoạt động tuần
   */
  loadWeeklyActivity(): void {
    this.isLoadingChart = true;

    this.dashboardService.getWeeklyActivity(5).subscribe({
      next: (data) => {
        console.log(' Đã tải dữ liệu biểu đồ tuần:', data);
        this.updateWeeklyChart(data);
        this.isLoadingChart = false;
        if (!this.isDestroyed) {
          this.cdr.detectChanges();
        }
      },
      error: (error) => {
        console.error(' Lỗi tải dữ liệu biểu đồ tuần:', error);
        this.isLoadingChart = false;
        if (!this.isDestroyed) {
          this.cdr.detectChanges();
        }
      }
    });
  }

  loadSlaBottleneck(): void {
    this.clearSlaTimers();
    const requestVersion = ++this.slaRequestVersion;

    this.isLoadingSla = true;
    if (!this.isDestroyed) {
      this.cdr.detectChanges();
    }

    const finishLoading = () => {
      if (requestVersion !== this.slaRequestVersion) {
        return;
      }

      this.clearSlaTimers();
      this.isLoadingSla = false;
      if (!this.isDestroyed) {
        this.cdr.detectChanges();
      }
    };

    const applyResponse = (response: { success: boolean; data: SlaDashboardDto }) => {
      if (requestVersion !== this.slaRequestVersion) {
        return;
      }

      if (response.success) {
        this.slaDashboard = response.data;
      }
      finishLoading();
    };

    const handleError = (error: any) => {
      if (requestVersion !== this.slaRequestVersion) {
        return;
      }

      console.error(' Lỗi tải dashboard SLA:', error);
      finishLoading();
    };

    const fireRequest = () => {
      this.dashboardService.getSlaDashboard(this.slaOnlyMy).subscribe({
        next: (response) => applyResponse(response),
        error: (error) => handleError(error)
      });
    };

    // Gọi lần đầu
    fireRequest();

    // Tự gọi lại 1 lần sau 2 giây nếu request đầu vẫn chưa xong.
    this.slaRetryTimer = setTimeout(() => {
      if (requestVersion !== this.slaRequestVersion || !this.isLoadingSla) {
        return;
      }
      fireRequest();
    }, 2000);

    // Giới hạn thời gian cứng để tránh quay loading vô hạn.
    this.slaTimeoutTimer = setTimeout(() => {
      if (requestVersion !== this.slaRequestVersion || !this.isLoadingSla) {
        return;
      }
      console.warn(' Request dashboard SLA bị quá thời gian chờ.');
      finishLoading();
    }, 10000);
  }

  toggleSlaScope(onlyMy: boolean): void {
    if (this.slaOnlyMy === onlyMy) {
      return;
    }

    this.slaOnlyMy = onlyMy;
    this.loadSlaBottleneck();
  }

  private clearSlaTimers(): void {
    if (this.slaRetryTimer) {
      clearTimeout(this.slaRetryTimer);
      this.slaRetryTimer = null;
    }
    if (this.slaTimeoutTimer) {
      clearTimeout(this.slaTimeoutTimer);
      this.slaTimeoutTimer = null;
    }
  }

  loadSlaAlerts(): void {
    this.isLoadingSlaAlerts = true;

    this.dashboardService.getSlaAlerts().subscribe({
      next: (data) => {
        console.log('🔔 Đã tải SLA alerts:', data);
        this.slaAlerts = data;
        this.isLoadingSlaAlerts = false;
        if (!this.isDestroyed) {
          this.cdr.detectChanges();
        }
      },
      error: (error) => {
        console.error('❌ Lỗi tải SLA alerts:', error);
        this.isLoadingSlaAlerts = false;
        if (!this.isDestroyed) {
          this.cdr.detectChanges();
        }
      }
    });
  }

  /**
  * Cập nhật biểu đồ tuần từ dữ liệu API
   */
  private updateWeeklyChart(data: WeeklyActivityDto): void {
    this.weeklyChartData.labels = data.labels;
    this.weeklyChartData.datasets[0].data = data.applicationsData;
    this.weeklyChartData.datasets[1].data = data.hiresData;
  }

  /**
  * Định dạng thời gian tương đối
   */
  getRelativeTime(timestamp: string): string {
    const utcStr = timestamp.endsWith('Z') ? timestamp : timestamp + 'Z';
    const date = new Date(utcStr);
    return date.toLocaleString('vi-VN', {
      hour: '2-digit',
      minute: '2-digit',
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  }

  /**
  * Định dạng ngày hiển thị
   */
  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }

  /**
  * Lấy biểu tượng tăng/giảm
   */
  getGrowthIcon(growth: number): string {
    return growth >= 0 ? 'trending_up' : 'trending_down';
  }

  /**
  * Lấy class màu theo tăng trưởng
   */
  getGrowthClass(growth: number): string {
    if (growth > 0) return 'text-green-600 bg-green-50 dark:bg-green-900/20';
    if (growth < 0) return 'text-red-600 bg-red-50 dark:bg-red-900/20';
    return 'text-gray-500 bg-gray-100 dark:bg-slate-800';
  }

  getRiskClass(level: string): string {
    switch (level) {
      case 'HIGH':
        return 'bg-red-100 text-red-700 border-red-200';
      case 'MEDIUM':
        return 'bg-amber-100 text-amber-700 border-amber-200';
      default:
        return 'bg-emerald-100 text-emerald-700 border-emerald-200';
    }
  }

  getRiskLabel(level: string): string {
    switch (level) {
      case 'HIGH':
        return 'Rủi ro cao';
      case 'MEDIUM':
        return 'Rủi ro trung bình';
      default:
        return 'Rủi ro thấp';
    }
  }

  getSlaStageDisplayName(stageName: string): string {
    const normalized = (stageName || '').trim().toLowerCase();

    switch (normalized) {
      case 'new applied':
      case 'application received':
        return 'Mới ứng tuyển';
      case 'screening':
        return 'Sàng lọc';
      case 'interview':
        return 'Phỏng vấn';
      case 'offer':
      case 'offer sent':
        return 'Đề nghị';
      case 'hired':
        return 'Đã tuyển';
      case 'rejected':
        return 'Đã từ chối';
      default:
        return stageName;
    }
  }
}

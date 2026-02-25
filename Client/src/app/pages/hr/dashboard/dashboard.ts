import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, OnDestroy } from '@angular/core';
import { Router, RouterLink, NavigationEnd } from '@angular/router';
import { JobService, JobDto } from '../../../services/job.service';
import { DashboardService, DashboardSummaryDto, DashboardActivityDto, DashboardCandidateDto, WeeklyActivityDto } from '../../../services/dashboard.service';
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

  // Dashboard Summary
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

  // Recent Activity
  activities: DashboardActivityDto[] = [];
  isLoadingActivities = false;

  // Latest Candidates
  candidates: DashboardCandidateDto[] = [];
  isLoadingCandidates = false;

  // Weekly Activity Chart
  public weeklyChartData: ChartData<'bar'> = {
    labels: [],
    datasets: [
      {
        data: [],
        label: 'Applications',
        backgroundColor: 'rgba(191, 219, 254, 0.8)', // blue-200
        borderColor: 'rgba(191, 219, 254, 1)',
        borderWidth: 1
      },
      {
        data: [],
        label: 'Hires',
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

  constructor(
    private jobService: JobService,
    private dashboardService: DashboardService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {
    // Register Chart.js components
    Chart.register(...registerables);
  }

  ngOnInit(): void {
    console.log('🔄 Dashboard ngOnInit called');
    this.loadAllDashboardData();

    // Pre-load jobs for Job List page
    this.jobService.refreshJobs();

    // Subscribe to router events để reload khi navigate back
    this.routerSubscription = this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: any) => {
        console.log('🔄 Navigation detected:', event.url);
        if (event.url === '/hr/dashboard' || event.url === '/hr') {
          console.log('🔄 Reloading dashboard data...');
          this.loadAllDashboardData();
        }
      });
  }

  ngOnDestroy(): void {
    this.routerSubscription?.unsubscribe();
  }

  /**
   * Load tất cả data cho dashboard
   */
  loadAllDashboardData(): void {
    this.loadSummary();
    this.loadRecentActivity();
    this.loadLatestCandidates();
    this.loadWeeklyActivity();
  }

  /**
   * Load summary statistics
   */
  loadSummary(): void {
    this.isLoadingSummary = true;

    this.dashboardService.getSummary().subscribe({
      next: (data) => {
        console.log('✅ Summary loaded:', data);
        this.summary = data;
        this.isLoadingSummary = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('❌ Error loading summary:', error);
        this.isLoadingSummary = false;
        this.cdr.detectChanges();
      }
    });
  }

  /**
   * Load recent activity
   */
  loadRecentActivity(): void {
    this.isLoadingActivities = true;

    this.dashboardService.getRecentActivity(10).subscribe({
      next: (data) => {
        console.log('✅ Activities loaded:', data);
        this.activities = data;
        this.isLoadingActivities = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('❌ Error loading activities:', error);
        this.isLoadingActivities = false;
        this.cdr.detectChanges();
      }
    });
  }

  /**
   * Load latest candidates
   */
  loadLatestCandidates(): void {
    this.isLoadingCandidates = true;

    this.dashboardService.getLatestCandidates(2).subscribe({
      next: (data) => {
        console.log('✅ Candidates loaded:', data);
        this.candidates = data;
        this.isLoadingCandidates = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('❌ Error loading candidates:', error);
        this.isLoadingCandidates = false;
        this.cdr.detectChanges();
      }
    });
  }

  /**
   * Load weekly activity chart data
   */
  loadWeeklyActivity(): void {
    this.isLoadingChart = true;

    this.dashboardService.getWeeklyActivity(5).subscribe({
      next: (data) => {
        console.log('✅ Weekly activity loaded:', data);
        this.updateWeeklyChart(data);
        this.isLoadingChart = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('❌ Error loading weekly activity:', error);
        this.isLoadingChart = false;
        this.cdr.detectChanges();
      }
    });
  }

  /**
   * Update weekly chart với data từ API
   */
  private updateWeeklyChart(data: WeeklyActivityDto): void {
    this.weeklyChartData.labels = data.labels;
    this.weeklyChartData.datasets[0].data = data.applicationsData;
    this.weeklyChartData.datasets[1].data = data.hiresData;
  }

  /**
   * Get relative time string (e.g., "2 mins ago")
   */
  getRelativeTime(timestamp: string): string {
    const now = new Date();
    const past = new Date(timestamp);
    const diffMs = now.getTime() - past.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'just now';
    if (diffMins < 60) return `${diffMins} mins ago`;
    if (diffHours < 24) return `${diffHours} ${diffHours === 1 ? 'hour' : 'hours'} ago`;
    return `${diffDays} ${diffDays === 1 ? 'day' : 'days'} ago`;
  }

  /**
   * Format date for display
   */
  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }

  /**
   * Get growth icon
   */
  getGrowthIcon(growth: number): string {
    return growth >= 0 ? 'trending_up' : 'trending_down';
  }

  /**
   * Get growth class
   */
  getGrowthClass(growth: number): string {
    if (growth > 0) return 'text-green-600 bg-green-50 dark:bg-green-900/20';
    if (growth < 0) return 'text-red-600 bg-red-50 dark:bg-red-900/20';
    return 'text-gray-500 bg-gray-100 dark:bg-slate-800';
  }
}

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// DTOs matching backend
export interface DashboardSummaryDto {
    totalCandidates: number;
    openPositions: number;
    interviewsToday: number;
    newApplications: number;
    totalCandidatesGrowth: number;
    openPositionsGrowth: number;
    interviewsTodayGrowth: number;
    newApplicationsGrowth: number;
}

export interface DashboardActivityDto {
    type: string;
    title: string;
    description: string;
    timestamp: string;
    icon: string;
    iconColor: string;
}

export interface DashboardCandidateDto {
    applicationId: string;
    jobId: string;
    candidateName: string;
    jobTitle: string;
    status: string;
    statusLabel: string;
    statusColor: string;
    appliedAt: string;
    avatarUrl?: string;
}

export interface WeeklyActivityDto {
    labels: string[];
    applicationsData: number[];
    hiresData: number[];
}

export interface SlaRecruiterBottleneckDto {
    recruiterId?: string;
    recruiterName: string;
    totalApplications: number;
    onTrackApplications: number;
    overdueApplications: number;
    warningApplications: number;
    severeOverdueApplications: number;
    complianceRate: number;
    slaHealthScore: number;
    riskLevel: 'LOW' | 'MEDIUM' | 'HIGH' | string;
    maxOverdueDays: number;
    avgOverdueDays: number;
}

export interface SlaStageBottleneckDto {
    stageName: string;
    totalApplications: number;
    onTrackApplications: number;
    overdueApplications: number;
    warningApplications: number;
    severeOverdueApplications: number;
    complianceRate: number;
    riskLevel: 'LOW' | 'MEDIUM' | 'HIGH' | string;
    maxOverdueDays: number;
    avgOverdueDays: number;
}

export interface SlaStuckApplicationDto {
    applicationId: string;
    candidateName: string;
    jobTitle: string;
    stageName: string;
    recruiterName: string;
    enteredStageAt: string;
    dueAt: string;
    overdueDays: number;
}

export interface SlaDashboardDto {
    totalTrackedApplications: number;
    onTrackApplications: number;
    overdueApplications: number;
    warningApplications: number;
    severeOverdueApplications: number;
    complianceRate: number;
    slaHealthScore: number;
    recruiters: SlaRecruiterBottleneckDto[];
    stages: SlaStageBottleneckDto[];
    topStuckApplications: SlaStuckApplicationDto[];
}

export interface ApiResponse<T> {
    success: boolean;
    data: T;
    message?: string;
}

@Injectable({
    providedIn: 'root'
})
export class DashboardService {
    private apiUrl = '/api/dashboard';

    constructor(private http: HttpClient) { }


    getSummary(): Observable<DashboardSummaryDto> {
        return this.http.get<DashboardSummaryDto>(`${this.apiUrl}/summary`);
    }

    getRecentActivity(count: number = 10): Observable<DashboardActivityDto[]> {
        return this.http.get<DashboardActivityDto[]>(`${this.apiUrl}/activity`, {
            params: { count: count.toString() }
        });
    }


    getPagedActivities(page: number = 1, pageSize: number = 15): Observable<{ totalItems: number, totalPages: number, currentPage: number, items: DashboardActivityDto[] }> {
        return this.http.get<{ totalItems: number, totalPages: number, currentPage: number, items: DashboardActivityDto[] }>(`${this.apiUrl}/activities`, {
            params: {
                page: page.toString(),
                pageSize: pageSize.toString()
            }
        });
    }


    getLatestCandidates(count: number = 10): Observable<DashboardCandidateDto[]> {
        return this.http.get<DashboardCandidateDto[]>(`${this.apiUrl}/candidates`, {
            params: { count: count.toString() }
        });
    }


    getWeeklyActivity(weeks: number = 5): Observable<WeeklyActivityDto> {
        return this.http.get<WeeklyActivityDto>(`${this.apiUrl}/weekly-activity`, {
            params: { weeks: weeks.toString() }
        });
    }

    /**
     * Lấy thông báo/hoạt động riêng của candidate
     * Backend sẽ kiểm tra quyền dựa trên JWT token
     * Đảm bảo candidate chỉ có thể xem thông báo của chính họ
     */
    getCandidateActivities(page: number = 1, pageSize: number = 15): Observable<{ totalItems: number, totalPages: number, currentPage: number, items: DashboardActivityDto[] }> {
        return this.http.get<{ totalItems: number, totalPages: number, currentPage: number, items: DashboardActivityDto[] }>(`${this.apiUrl}/candidate-activities`, {
            params: {
                page: page.toString(),
                pageSize: pageSize.toString()
            }
        });
    }

    /**
     * Lấy số lượng thông báo chưa đọc của candidate
     */
    getCandidateUnreadCount(): Observable<{ unreadCount: number }> {
        return this.http.get<{ unreadCount: number }>(`${this.apiUrl}/candidate-unread-count`);
    }

    getSlaDashboard(onlyMy = false): Observable<ApiResponse<SlaDashboardDto>> {
        return this.http.get<ApiResponse<SlaDashboardDto>>('/api/sla/dashboard', {
            params: {
                onlyMy: String(onlyMy)
            }
        });
    }

    /**
     * Lấy SLA alerts/notifications từ API
     */
    getSlaAlerts(): Observable<DashboardActivityDto[]> {
        return this.http.get<DashboardActivityDto[]>(`${this.apiUrl}/sla-alerts`);
    }
}

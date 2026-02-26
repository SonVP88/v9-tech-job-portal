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

@Injectable({
    providedIn: 'root'
})
export class DashboardService {
    private apiUrl = '/api/dashboard';

    constructor(private http: HttpClient) { }

    /**
     * Lấy summary statistics cho 4 cards
     */
    getSummary(): Observable<DashboardSummaryDto> {
        return this.http.get<DashboardSummaryDto>(`${this.apiUrl}/summary`);
    }

    getRecentActivity(count: number = 10): Observable<DashboardActivityDto[]> {
        return this.http.get<DashboardActivityDto[]>(`${this.apiUrl}/activity`, {
            params: { count: count.toString() }
        });
    }

    /**
     * Lấy recent activity log phân trang
     */
    getPagedActivities(page: number = 1, pageSize: number = 15): Observable<{ totalItems: number, totalPages: number, currentPage: number, items: DashboardActivityDto[] }> {
        return this.http.get<{ totalItems: number, totalPages: number, currentPage: number, items: DashboardActivityDto[] }>(`${this.apiUrl}/activities`, {
            params: {
                page: page.toString(),
                pageSize: pageSize.toString()
            }
        });
    }

    /**
     * Lấy latest candidates cho table
     */
    getLatestCandidates(count: number = 10): Observable<DashboardCandidateDto[]> {
        return this.http.get<DashboardCandidateDto[]>(`${this.apiUrl}/candidates`, {
            params: { count: count.toString() }
        });
    }

    /**
     * Lấy weekly activity data cho chart
     */
    getWeeklyActivity(weeks: number = 5): Observable<WeeklyActivityDto> {
        return this.http.get<WeeklyActivityDto>(`${this.apiUrl}/weekly-activity`, {
            params: { weeks: weeks.toString() }
        });
    }
}

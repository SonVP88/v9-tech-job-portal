import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { isPlatformBrowser } from '@angular/common';

// DTO for interview schedule
export interface MyInterviewDto {
    interviewId: string;
    interviewerId: string;
    candidateName: string;
    jobTitle: string;
    position: string;
    interviewTime: string; // ISO DateTime
    formattedTime: string; // "10:00 AM"
    formattedDate: string; // "15/01/2026"
    location: string;
    meetingLink?: string;
    locationType: 'online' | 'offline';
    status: 'upcoming' | 'ongoing' | 'completed';
    candidateEmail?: string;
    candidatePhone?: string;
    interviewerName?: string;
    interviewerEmail?: string;
}

export interface ApiResponse<T> {
    success: boolean;
    data: T;
    message?: string;
}


@Injectable({
    providedIn: 'root'
})
export class InterviewService {
    private apiUrl = '/api/interviews';
    private platformId = inject(PLATFORM_ID);

    constructor(private http: HttpClient) { }

    /**
     * Lấy danh sách lịch phỏng vấn cá nhân (SECURE: UserId được lấy từ JWT Token ở Backend)
     * KHÔNG truyền userId qua tham số để tránh IDOR vulnerability
     */
    getMyInterviews(): Observable<ApiResponse<MyInterviewDto[]>> {
        // ⚡ SSR Fix: Only access localStorage in browser
        if (!isPlatformBrowser(this.platformId)) {
            console.log('⏭️ SSR: Skipping API call requiring token');
            return of({ success: false, data: [] as MyInterviewDto[], message: 'SSR: Skipping call' });
        }

        const token = localStorage.getItem('authToken');
        const headers = new HttpHeaders({
            'Authorization': `Bearer ${token}`
        });

        return this.http.get<ApiResponse<MyInterviewDto[]>>(
            `${this.apiUrl}/my-schedule`,
            { headers }
        );
    }
}

import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { isPlatformBrowser } from '@angular/common';

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

    getMyInterviews(): Observable<ApiResponse<MyInterviewDto[]>> {
        if (!isPlatformBrowser(this.platformId)) {
            return of({ success: false, data: [] as MyInterviewDto[], message: 'SSR: Skipping call' });
        }

        // authInterceptor sẽ tự động đính kèm token chuẩn xác
        return this.http.get<ApiResponse<MyInterviewDto[]>>(`${this.apiUrl}/my-schedule`);
    }
}

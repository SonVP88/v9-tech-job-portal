import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap, filter, take, map, concatMap, finalize } from 'rxjs/operators';

// Interface cho CreateJobRequest
export interface CreateJobRequest {
    title: string;
    description?: string;
    requirements?: string;
    benefits?: string;
    numberOfPositions?: number;
    salaryMin?: number;
    salaryMax?: number;
    location?: string;
    employmentType?: string;
    deadline?: string; // ISO date string
    skillIds: string[];
}

export interface CreateJobResponse {
    message: string;
}

export interface JobDto {
    jobId: string;
    title: string;
    companyName?: string;
    createdByName?: string;
    createdByRole?: string;
    location?: string;
    salaryMin?: number;
    salaryMax?: number;
    employmentType?: string;
    deadline?: string;
    createdDate: string;
    status?: string;
    skills?: string[];
    numberOfPositions?: number;
    totalHired?: number;
    // Audit Trail
    closedAt?: string;
    closedByName?: string;
}

export interface JobDetailDto extends JobDto {
    description?: string;
    requirements?: string;
    benefits?: string;
    contactEmail?: string;
    numberOfPositions?: number;
    skillIds?: string[];
}

@Injectable({
    providedIn: 'root'
})
export class JobService {
    private apiUrl = '/api';

    private jobsSubject = new BehaviorSubject<JobDto[] | null>(null);
    public jobs$ = this.jobsSubject.asObservable();
    private hasLoaded = false;

    constructor(private http: HttpClient) { }

    /**
     * Tạo job posting mới
     */
    createJob(jobData: CreateJobRequest): Observable<CreateJobResponse> {
        return this.http.post<CreateJobResponse>(`${this.apiUrl}/jobs`, jobData);
    }

    /**
     * Lấy danh sách tất cả các job (cho admin quản lý)
     */
    getAllJobs(forceRefresh = false): Observable<JobDto[]> {
        if (forceRefresh || !this.hasLoaded || !this.jobsSubject.value) {
            return this.fetchJobs();
        }
        return this.jobs$.pipe(
            filter((jobs): jobs is JobDto[] => jobs !== null),
            take(1)
        );
    }

    /**
     * Fetch jobs from API and update subject
     */
    fetchJobs(): Observable<JobDto[]> {
        const startTime = performance.now();
        console.log(`[JobService] 🕒 Bắt đầu gọi API: /api/jobs at ${new Date().toLocaleTimeString()}`);

        return this.http.get<JobDto[]>(`${this.apiUrl}/jobs`).pipe(
            tap(jobs => {
                const endTime = performance.now();
                console.log(`[JobService] ✅ Đã nhận dữ liệu từ API. Số lượng: ${jobs.length}`);
                console.log(`[JobService] ⏱️ Thời gian phản hồi API: ${(endTime - startTime).toFixed(2)}ms`);
                this.jobsSubject.next(jobs);
                this.hasLoaded = true;
            })
        );
    }

    /**
     * Refresh jobs in background
     */
    refreshJobs(): void {
        this.fetchJobs().subscribe();
    }

    /**
     * Cập nhật job posting
     */
    updateJob(id: string, jobData: CreateJobRequest): Observable<any> {
        return this.http.put<any>(`${this.apiUrl}/jobs/${id}`, jobData);
    }

    /**
     * Xóa job posting
     */
    deleteJob(id: string): Observable<any> {
        return this.http.delete<any>(`${this.apiUrl}/jobs/${id}`);
    }

    closeJob(id: string): Observable<any> {
        return this.http.put<any>(`${this.apiUrl}/jobs/${id}/close`, {});
    }

    openJob(id: string): Observable<any> {
        return this.http.put<any>(`${this.apiUrl}/jobs/${id}/open`, {});
    }

    /**
     * Lấy danh sách jobs mới nhất
     */
    getLatestJobs(count: number = 10, keyword?: string, location?: string): Observable<JobDto[]> {
        let params = new HttpParams();
        if (keyword) params = params.set('keyword', keyword);
        if (location) params = params.set('location', location);

        return this.http.get<JobDto[]>(`${this.apiUrl}/jobs/latest/${count}`, { params });
    }

    getJobById(id: string): Observable<JobDetailDto> {
        return this.http.get<JobDetailDto>(`${this.apiUrl}/jobs/${id}`);
    }

    getSystemStats(): Observable<any> {
        return this.http.get<any>(`${this.apiUrl}/public/jobs/stats`);
    }

    searchPublicJobs(keyword?: string, location?: string, jobType?: string, minSalary?: number): Observable<JobDto[]> {
        let params = new HttpParams();
        if (keyword) params = params.set('keyword', keyword);
        if (location) params = params.set('location', location);
        if (jobType) params = params.set('jobType', jobType);
        if (minSalary) params = params.set('minSalary', minSalary.toString());

        return this.http.get<JobDto[]>(`${this.apiUrl}/public/jobs/search`, { params });
    }

    getRecommendedJobs(top: number = 10): Observable<any[]> {
        return this.http.get<any[]>(`${this.apiUrl}/recommendation/jobs?top=${top}`);
    }

    getRecommendedCandidates(jobId: string, top: number = 10): Observable<any[]> {
        return this.http.get<any[]>(`${this.apiUrl}/recommendation/candidates/${jobId}?top=${top}`);
    }
}

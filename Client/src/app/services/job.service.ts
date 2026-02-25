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

// Interface cho response
export interface CreateJobResponse {
    message: string;
}

// Interface cho Job DTO (matches backend camelCase response)
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

    // State management for Jobs
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
     * Returns cached data if available, otherwise fetches from API
     */
    getAllJobs(forceRefresh = false): Observable<JobDto[]> {
        // Always fetch fresh data on force refresh or if not loaded yet
        if (forceRefresh || !this.hasLoaded || !this.jobsSubject.value) {
            return this.fetchJobs();
        }
        // Return cached data
        return this.jobs$.pipe(
            filter((jobs): jobs is JobDto[] => jobs !== null),
            take(1)
        );
    }

    /**
     * Fetch jobs from API and update subject
     */
    fetchJobs(): Observable<JobDto[]> {
        return this.http.get<JobDto[]>(`${this.apiUrl}/jobs`).pipe(
            tap(jobs => {
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
     * Hỗ trợ tìm kiếm theo keyword và location
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
}

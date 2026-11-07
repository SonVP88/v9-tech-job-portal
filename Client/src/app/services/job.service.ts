import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap, filter, take, map, concatMap, finalize } from 'rxjs/operators';

// Interface cho CreateJobRequest
export interface CreateJobRequest {
    title: string;
    departmentId?: string;
    description?: string;
    requirements?: string;
    benefits?: string;
    numberOfPositions?: number;
    salaryMin?: number;
    salaryMax?: number;
    location?: string;
    employmentType?: string;
    experienceLevel?: string;
    seniorityLevel?: string;
    deadline?: string; // ISO date string
    skillIds: string[];
    createCampaignQr?: boolean;
    requireNightShift?: boolean;
    requisitionStatus?: string;
    requisitionNote?: string;
    recruitmentTrack?: string;
    targetQuantity?: number;
}

export interface CampaignQrDto {
    jobId: string;
    jobTitle: string;
    source: string;
    googleFormId?: string;
    formUrl: string;
    formEditUrl?: string;
    qrCodeDataUrl: string;
    qrCodePath?: string;
    warning?: string;
    appsScript?: string;
    publicApiBaseUrl?: string;
    publicClientBaseUrl?: string;
    webhookUrl?: string;
    webhookSecret?: string;
    isUsingPublicUrl?: boolean;
}

export interface CreateJobResponse {
    success?: boolean;
    jobId?: string;
    message: string;
    campaign?: CampaignQrDto;
}

export interface AiGenerateJdRequest {
    title: string;
    level?: string;
    keyRequirements?: string;
    salaryMin?: number;
    salaryMax?: number;
    currency?: string;
}

export interface AiGenerateJdResult {
    description: string;
    requirements: string;
    benefits: string;
}

export interface JobDto {
    jobId: string;
    title: string;
    departmentId?: string;
    departmentName?: string;
    companyName?: string;
    createdByName?: string;
    createdByRole?: string;
    location?: string;
    salaryMin?: number;
    salaryMax?: number;
    employmentType?: string;
    experienceLevel?: string;
    seniorityLevel?: string;
    deadline?: string;
    createdDate: string;
    status?: string;
    skills?: string[];
    numberOfPositions?: number;
    totalHired?: number;
    // Audit Trail
    closedAt?: string;
    closedByName?: string;
    // Candidate Application Status (NEW)
    hasApplied?: boolean;
    appliedAt?: string;
    campaignFormSource?: string;
    googleFormPublicUrl?: string;
    qrCodeImagePath?: string;

    // V3 Enterprise Compliance (Phê duyệt định biên & ca đêm)
    recruitmentTrack?: string;
    targetQuantity?: number;
    hiredCount?: number;
    waitlistBufferPercent?: number;
    requisitionStatus?: string; // 'Draft' | 'Pending_Approval' | 'Approved' | 'Rejected_Budget'
    requisitionNote?: string;
    requireNightShift?: boolean;
    approvedAt?: string;
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

    /**
     * Gọi AI để sinh nội dung Job Description (description, requirements, benefits)
     */
    generateJd(request: AiGenerateJdRequest): Observable<AiGenerateJdResult> {
        return this.http.post<AiGenerateJdResult>(`${this.apiUrl}/jobs/ai-generate-jd`, request);
    }

    getCampaignQr(jobId: string, publicApiUrl?: string, publicClientUrl?: string): Observable<CampaignQrDto> {
        let params = new HttpParams();
        if (publicApiUrl) params = params.set('publicApiUrl', publicApiUrl);
        if (publicClientUrl) params = params.set('publicClientUrl', publicClientUrl);
        return this.http.get<CampaignQrDto>(`${this.apiUrl}/jobs/${jobId}/campaign-qr`, { params });
    }

    ensureCampaignQr(jobId: string, force = false, publicApiUrl?: string, publicClientUrl?: string): Observable<CampaignQrDto> {
        let params = new HttpParams().set('force', String(force));
        if (publicApiUrl) params = params.set('publicApiUrl', publicApiUrl);
        if (publicClientUrl) params = params.set('publicClientUrl', publicClientUrl);
        return this.http.post<CampaignQrDto>(`${this.apiUrl}/jobs/${jobId}/campaign-qr`, {}, { params });
    }

    updateCampaignPublicConfig(jobId: string, config: { publicApiBaseUrl?: string; publicClientBaseUrl?: string; webhookSecret?: string }): Observable<CampaignQrDto> {
        return this.http.put<CampaignQrDto>(`${this.apiUrl}/jobs/${jobId}/campaign-qr/public-config`, config);
    }

    updateCustomFormUrl(jobId: string, formUrl: string): Observable<CampaignQrDto> {
        return this.http.put<CampaignQrDto>(`${this.apiUrl}/jobs/${jobId}/campaign-qr/custom-form`, { formUrl });
    }

    /**
     * Phê duyệt định biên ngân sách chiến dịch (RequisitionStatus = 'Approved')
     */
    approveRequisition(jobId: string): Observable<{ success: boolean; message: string }> {
        return this.http.post<{ success: boolean; message: string }>(`${this.apiUrl}/jobs/${jobId}/approve-requisition`, {});
    }
}

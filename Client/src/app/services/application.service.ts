import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ApplicationDto {
    applicationId: string;
    candidateId: string;   // Thêm để gọi API generate-opening
    candidateName: string;
    email: string;
    phone: string;
    appliedAt: string;
    cvUrl: string;
    status: string;
    jobId: string;         // Thêm để xác định job khi view all
    matchScore?: number;
    aiExplanation?: string;
    jobTitle?: string;     // Thêm để hiển thị trong email
    currentStageCode?: string;
    currentStageName?: string;
    slaDueAt?: string;
    slaStatus?: 'ON_TRACK' | 'WARNING' | 'OVERDUE' | 'DISABLED';
    slaOverdueDays?: number;
    slaMaxDays?: number;
    slaWarnBeforeDays?: number;
}

export interface SlaStageConfigDto {
    stageId: string;
    code: string;
    name: string;
    sortOrder: number;
    isTerminal: boolean;
    isSlaEnabled: boolean;
    slaMaxDays?: number;
    slaWarnBeforeDays?: number;
}

export interface UpdateSlaStageConfigRequest {
    isSlaEnabled: boolean;
    slaMaxDays?: number;
    slaWarnBeforeDays?: number;
}

export interface SlaRecruiterBottleneckDto {
    recruiterId?: string;
    recruiterName: string;
    totalApplications: number;
    overdueApplications: number;
    warningApplications: number;
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
    overdueApplications: number;
    warningApplications: number;
    recruiters: SlaRecruiterBottleneckDto[];
    topStuckApplications: SlaStuckApplicationDto[];
}

export interface MyApplicationDto {
    applicationId: string;
    jobId: string;
    jobTitle: string;
    companyName: string;
    jobLocation?: string;
    appliedAt: string;
    status: string;
    lastViewedAt?: string;
    cvUrl?: string;
}

export interface OfferDetailDto {
    applicationId: string;
    candidateName: string;
    position: string;
    salary: number;
    startDate: string;
    expiryDate: string;
    contractType: string;
    offerSentAt: string;
}

export interface ApiResponse<T> {
    success: boolean;
    data: T;
    message?: string;
}

export interface CandidateSkillDto {
    skillId: string;
    skillName: string;
    level?: string;
    years?: number;
}

export interface CandidateDocumentDto {
    documentId: string;
    fileName: string;
    fileUrl: string;
    docType: string;
    sizeBytes?: number;
    createdAt: string;
    isPrimary: boolean;
    displayName: string;
}

export interface CandidateProfileDto {
    candidateId: string;
    fullName: string;
    email: string;
    phone?: string;
    location?: string;
    headline?: string;
    summary?: string;
    linkedIn?: string;
    gitHub?: string;
    avatar?: string;
    skills: CandidateSkillDto[];
    documents: CandidateDocumentDto[];
}



@Injectable({
    providedIn: 'root'
})
export class ApplicationService {
    private apiUrl = '/api';

    constructor(private http: HttpClient) { }

    /**
     * Lấy danh sách hồ sơ theo JobId (Dành cho HR)
     */
    getApplicationsByJobId(jobId: string): Observable<ApiResponse<ApplicationDto[]>> {
        return this.http.get<ApiResponse<ApplicationDto[]>>(`${this.apiUrl}/jobs/${jobId}/applications`);
    }

    /**
     * Cập nhật trạng thái hồ sơ ứng viên (Dành cho HR)
     */
    updateApplicationStatus(applicationId: string, status: string): Observable<ApiResponse<any>> {
        return this.http.put<ApiResponse<any>>(`${this.apiUrl}/applications/${applicationId}/status?status=${status}`, {});
    }

    /**
     * Lấy toàn bộ danh sách hồ sơ ứng tuyển (Dành cho HR)
     */
    getAllApplications(): Observable<ApiResponse<ApplicationDto[]>> {
        return this.http.get<ApiResponse<ApplicationDto[]>>(`${this.apiUrl}/applications`);
    }

    /**
     * Lấy danh sách hồ sơ đã nộp của ứng viên đang đăng nhập
     */
    getMyApplications(): Observable<ApiResponse<MyApplicationDto[]>> {
        return this.http.get<ApiResponse<MyApplicationDto[]>>(`${this.apiUrl}/applications/my-applications`);
    }

    /**
     * Ứng viên phản hồi Offer: true = Đồng ý (OFFER_ACCEPTED), false = Từ chối (REJECTED)
     */
    respondToOffer(applicationId: string, accept: boolean): Observable<ApiResponse<any>> {
        return this.http.put<ApiResponse<any>>(
            `${this.apiUrl}/applications/${applicationId}/respond-offer?accept=${accept}`,
            {}
        );
    }

    /**
     * Ứng viên xem lại chi tiết Offer đã gửi cho hồ sơ.
     */
    getOfferDetail(applicationId: string): Observable<ApiResponse<OfferDetailDto>> {
        return this.http.get<ApiResponse<OfferDetailDto>>(`${this.apiUrl}/offer/application/${applicationId}/detail`);
    }

    getSlaStageConfigs(): Observable<ApiResponse<SlaStageConfigDto[]>> {
        return this.http.get<ApiResponse<SlaStageConfigDto[]>>(`${this.apiUrl}/sla/stages`);
    }

    updateSlaStageConfig(stageId: string, request: UpdateSlaStageConfigRequest): Observable<ApiResponse<any>> {
        return this.http.put<ApiResponse<any>>(`${this.apiUrl}/sla/stages/${stageId}`, request);
    }

    getSlaDashboard(onlyMy = false): Observable<ApiResponse<SlaDashboardDto>> {
        return this.http.get<ApiResponse<SlaDashboardDto>>(`${this.apiUrl}/sla/dashboard`, {
            params: {
                onlyMy: String(onlyMy)
            }
        });
    }

    /**
     * HR/Admin lấy profile ứng viên theo candidateId
     */
    getCandidateProfile(candidateId: string): Observable<CandidateProfileDto> {
        return this.http.get<CandidateProfileDto>(`${this.apiUrl}/candidate/profile/${candidateId}`);
    }

    /**
     * Ghi nhận lượt xem CV (Dành cho HR/Admin)
     */
    trackCvView(applicationId: string): Observable<ApiResponse<any>> {
        return this.http.post<ApiResponse<any>>(`${this.apiUrl}/applications/${applicationId}/track-view`, {});
    }
}

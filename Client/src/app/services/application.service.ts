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
     * Ứng viên phản hồi Offer: true = Đồng ý (HIRED), false = Từ chối (REJECTED)
     */
    respondToOffer(applicationId: string, accept: boolean): Observable<ApiResponse<any>> {
        return this.http.put<ApiResponse<any>>(
            `${this.apiUrl}/applications/${applicationId}/respond-offer?accept=${accept}`,
            {}
        );
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

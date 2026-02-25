import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CandidateSkillDto {
    skillId: string;
    skillName: string;
    level?: number;
    years?: number;
}

export interface CandidateDocumentDto {
    documentId: string;
    fileName: string;
    fileUrl: string;
    docType: string;
    sizeBytes?: number;
    createdAt: Date;
    isPrimary?: boolean;
    displayName?: string;
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

export interface UpdateCandidateProfileDto {
    fullName: string;
    phone?: string;
    location?: string;
    headline?: string;
    summary?: string;
    linkedIn?: string;
    gitHub?: string;
    avatar?: string;
    skillIds: string[];
    skills?: string[];
}

@Injectable({
    providedIn: 'root'
})
export class CandidateService {
    private apiUrl = 'https://localhost:7181/api/candidate';

    constructor(private http: HttpClient) { }

    getProfile(): Observable<CandidateProfileDto> {
        return this.http.get<CandidateProfileDto>(`${this.apiUrl}/profile?t=${new Date().getTime()}`);
    }

    updateProfile(dto: UpdateCandidateProfileDto): Observable<any> {
        return this.http.put(`${this.apiUrl}/profile`, dto);
    }

    uploadCV(file: File): Observable<any> {
        const formData = new FormData();
        formData.append('file', file);
        return this.http.post(`${this.apiUrl}/upload-cv`, formData);
    }

    deleteCV(documentId: string): Observable<any> {
        return this.http.delete(`${this.apiUrl}/cv/${documentId}`);
    }

    uploadAvatar(file: File): Observable<any> {
        const formData = new FormData();
        formData.append('file', file);
        return this.http.post(`${this.apiUrl}/upload-avatar`, formData);
    }

    setPrimaryDocument(documentId: string): Observable<any> {
        return this.http.put(`${this.apiUrl}/cv/${documentId}/primary`, {});
    }

    renameDocument(documentId: string, newName: string): Observable<any> {
        return this.http.put(`${this.apiUrl}/cv/${documentId}/rename`, { newName });
    }

    downloadFile(url: string): Observable<Blob> {
        return this.http.get(url, { responseType: 'blob' });
    }

    changePassword(data: any): Observable<any> {
        return this.http.post('https://localhost:7181/api/auth/change-password', data);
    }

    getNotificationSettings(): Observable<NotificationSettingDto> {
        return this.http.get<NotificationSettingDto>('https://localhost:7181/api/notification-settings');
    }

    updateNotificationSettings(settings: NotificationSettingDto): Observable<NotificationSettingDto> {
        return this.http.put<NotificationSettingDto>('https://localhost:7181/api/notification-settings', settings);
    }
}

export interface NotificationSettingDto {
    notifyJobOpportunities: boolean;
    notifyApplicationUpdates: boolean;
    notifySecurityAlerts: boolean;
    notifyMarketing: boolean;
    channelEmail: boolean;
    channelPush: boolean;
}

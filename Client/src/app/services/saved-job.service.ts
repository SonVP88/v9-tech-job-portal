import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface SavedJobDto {
    savedJobId: string;
    savedAt: string;
    job: {
        jobId: string;
        title: string;
        companyName: string;
        location: string;
        employmentType: string;
        salaryMin: number;
        salaryMax: number;
        currency: string;
        status: string;
        skills: string[];
        createdAt: string;
    };
}

@Injectable({ providedIn: 'root' })
export class SavedJobService {
    private apiUrl = `${environment.apiUrl}/candidate/saved-jobs`;

    constructor(private http: HttpClient) { }

    private getHeaders(): HttpHeaders {
        const token = localStorage.getItem('authToken') || '';
        return new HttpHeaders({ Authorization: `Bearer ${token}` });
    }

    getSavedJobs(): Observable<SavedJobDto[]> {
        return this.http.get<SavedJobDto[]>(this.apiUrl, { headers: this.getHeaders() });
    }

    getSavedJobIds(): Observable<string[]> {
        return this.http.get<string[]>(`${this.apiUrl}/ids`, { headers: this.getHeaders() });
    }

    toggleSave(jobId: string): Observable<{ saved: boolean; message: string }> {
        return this.http.post<{ saved: boolean; message: string }>(
            `${this.apiUrl}/${jobId}`,
            {},
            { headers: this.getHeaders() }
        );
    }

    checkSaved(jobId: string): Observable<{ saved: boolean }> {
        return this.http.get<{ saved: boolean }>(`${this.apiUrl}/check/${jobId}`, { headers: this.getHeaders() });
    }
}

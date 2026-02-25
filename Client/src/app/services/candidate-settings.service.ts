import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface NotificationSettings {
    notifyJobOpportunities: boolean;
    notifyApplicationUpdates: boolean;
    notifySecurityAlerts: boolean;
    notifyMarketing: boolean;
    channelEmail: boolean;
    channelPush: boolean;
}

@Injectable({ providedIn: 'root' })
export class CandidateSettingsService {
    private apiUrl = `${environment.apiUrl}/candidate/settings`;

    constructor(private http: HttpClient) { }

    private getHeaders(): HttpHeaders {
        const token = localStorage.getItem('authToken') || '';
        return new HttpHeaders({ Authorization: `Bearer ${token}` });
    }

    changePassword(current: string, newPwd: string, confirm: string): Observable<{ message: string }> {
        return this.http.post<{ message: string }>(
            `${this.apiUrl}/change-password`,
            { currentPassword: current, newPassword: newPwd, confirmPassword: confirm },
            { headers: this.getHeaders() }
        );
    }

    getNotificationSettings(): Observable<NotificationSettings> {
        return this.http.get<NotificationSettings>(`${this.apiUrl}/notifications`, { headers: this.getHeaders() });
    }

    updateNotificationSettings(settings: NotificationSettings): Observable<{ message: string }> {
        return this.http.put<{ message: string }>(`${this.apiUrl}/notifications`, settings, { headers: this.getHeaders() });
    }
}

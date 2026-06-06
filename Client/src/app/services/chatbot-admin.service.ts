import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ChatbotFaqItem {
    faqId: string;
    question: string;
    answer: string;
    category: string;
    keywords?: string;
    priority: number;
    isActive: boolean;
    createdAt: string;
    updatedAt?: string;
}

export interface UpsertChatbotFaqPayload {
    question: string;
    answer: string;
    category: string;
    keywords?: string;
    priority: number;
    isActive: boolean;
}

export interface GeminiKeyStat {
    keyIndex: number;
    keyPreview: string;
    successCount: number;
    failureCount: number;
    disabled: boolean;
    assignedModules?: string[];
}

@Injectable({
    providedIn: 'root'
})
export class ChatbotAdminService {
    private apiUrl = `${environment.apiUrl}/chatbot/admin/faqs`;

    constructor(private http: HttpClient) { }

    private getHeaders(): HttpHeaders {
        const token = localStorage.getItem('authToken') || '';
        return new HttpHeaders({
            Authorization: `Bearer ${token}`,
            'Content-Type': 'application/json'
        });
    }

    getFaqs(search = '', isActive?: boolean): Observable<ChatbotFaqItem[]> {
        let params = new HttpParams();
        if (search.trim()) {
            params = params.set('search', search.trim());
        }
        if (typeof isActive === 'boolean') {
            params = params.set('isActive', isActive);
        }

        return this.http.get<ChatbotFaqItem[]>(this.apiUrl, {
            headers: this.getHeaders(),
            params
        });
    }

    createFaq(payload: UpsertChatbotFaqPayload): Observable<any> {
        return this.http.post(this.apiUrl, payload, { headers: this.getHeaders() });
    }

    updateFaq(faqId: string, payload: UpsertChatbotFaqPayload): Observable<any> {
        return this.http.put(`${this.apiUrl}/${faqId}`, payload, { headers: this.getHeaders() });
    }

    toggleFaq(faqId: string): Observable<any> {
        return this.http.patch(`${this.apiUrl}/${faqId}/toggle`, {}, { headers: this.getHeaders() });
    }

    deleteFaq(faqId: string): Observable<any> {
        return this.http.delete(`${this.apiUrl}/${faqId}`, { headers: this.getHeaders() });
    }

    getGeminiKeys(): Observable<GeminiKeyStat[]> {
        return this.http.get<GeminiKeyStat[]>(`${environment.apiUrl}/admin/gemini-keys`, { headers: this.getHeaders() });
    }

    disableGeminiKey(keyIndex: number): Observable<any> {
        return this.http.post(`${environment.apiUrl}/admin/gemini-keys/${keyIndex}/disable`, {}, { headers: this.getHeaders() });
    }

    enableGeminiKey(keyIndex: number): Observable<any> {
        return this.http.post(`${environment.apiUrl}/admin/gemini-keys/${keyIndex}/enable`, {}, { headers: this.getHeaders() });
    }

    addGeminiKey(key: string): Observable<any> {
        return this.http.post(`${environment.apiUrl}/admin/gemini-keys/add`, { key }, { headers: this.getHeaders() });
    }
}

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SendOfferLetterDto {
    applicationId: string;
    candidateName: string;
    candidateEmail: string;
    position: string;
    salary: number;
    startDate: string;
    expiryDate: string;
    contractType: string;
    ccInterviewer: boolean;
    additionalCcEmails: string;
}

@Injectable({
    providedIn: 'root'
})
export class OfferService {
    private apiUrl = '/api/offer';

    constructor(private http: HttpClient) { }

    /**
     * Gửi Offer Letter qua email
     */
    sendOfferLetter(payload: SendOfferLetterDto): Observable<any> {
        return this.http.post(`${this.apiUrl}/send-offer-letter`, payload);
    }
}

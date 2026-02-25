import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CompanyInfoDto {
    name: string;
    website: string;
    industry: string;
    address: string;
    description: string;
    logoUrl: string;
}

@Injectable({
    providedIn: 'root'
})
export class CompanyService {
    private apiUrl = `${environment.apiUrl}/account/company-info`;

    constructor(private http: HttpClient) { }

    /**
     * Get public company information (no auth required)
     */
    getPublicCompanyInfo(): Observable<CompanyInfoDto> {
        return this.http.get<CompanyInfoDto>(this.apiUrl);
    }
}

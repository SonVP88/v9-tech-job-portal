import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { timeout } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface JobPublic {
    jobId: string;
    title: string;
    companyName: string;
    companyLogo: string | null;
    location: string;
    salaryMin?: number;
    salaryMax?: number;
    salaryRange: string;
    jobType: string;
    createdAt: Date;
    skills: string[];
}

export interface JobSearchFilter {
    keyword: string;
    location: string;
    jobType: string;
    minSalary?: number;
}

@Injectable({
    providedIn: 'root'
})
export class PublicJobService {
    private apiUrl = `${environment.apiUrl}/public/jobs`;

    constructor(private http: HttpClient) {
        console.log('🌐 PublicJobService initialized with API URL:', this.apiUrl);
    }

    searchJobs(filter: JobSearchFilter): Observable<JobPublic[]> {
        let params = new HttpParams();

        if (filter.keyword) params = params.set('keyword', filter.keyword);
        if (filter.location) params = params.set('location', filter.location);
        if (filter.jobType) params = params.set('jobType', filter.jobType);
        if (filter.minSalary) params = params.set('minSalary', filter.minSalary.toString());

        const url = `${this.apiUrl}/search`;
        console.log('🚀 Making request to:', url, 'with params:', params.toString());

        return this.http.get<JobPublic[]>(url, { params }).pipe(
            timeout(30000) // 30 second timeout
        );
    }
}


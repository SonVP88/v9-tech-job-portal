import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// Interface cho JobType
export interface JobType {
    jobTypeId: string;
    name: string;
}

// Interface cho Skill
export interface Skill {
    skillId: string;
    name: string;
}

// Interface cho Province/City
export interface Province {
    code: number;  // Backend trả về int
    name: string;
    fullName: string;
    nameEn?: string;
}

// Ward DTO - V2 API bỏ cấp huyện, wards thuộc trực tiếp province
export interface Ward {
    code: number;  // Backend trả về int
    name: string;
    provinceCode: number;  // Thuộc tỉnh, không còn district
}

@Injectable({
    providedIn: 'root'
})
export class MasterDataService {
    private apiUrl = '/api';

    constructor(private http: HttpClient) { }

    /**
     * Lấy danh sách JobTypes từ API
     */
    getJobTypes(): Observable<JobType[]> {
        return this.http.get<JobType[]>(`${this.apiUrl}/master-data/job-types`);
    }

    /**
     * Lấy danh sách Skills từ API
     */
    getSkills(): Observable<Skill[]> {
        return this.http.get<Skill[]>(`${this.apiUrl}/master-data/skills`);
    }

    /**
     * Lấy danh sách tỉnh/thành phố
     */
    getProvinces(): Observable<Province[]> {
        return this.http.get<Province[]>(`${this.apiUrl}/master-data/provinces`);
    }

    /**
     * Lấy danh sách phường/xã theo mã tỉnh (V2 API - bỏ cấp huyện)
     */
    getWards(provinceCode: number): Observable<Ward[]> {
        return this.http.get<Ward[]>(`${this.apiUrl}/master-data/provinces/${provinceCode}/wards`);
    }
}

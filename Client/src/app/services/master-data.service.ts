import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface JobType {
    jobTypeId: string;
    name: string;
}

export interface Skill {
    skillId: string;
    name: string;
}

export interface Province {
    code: number;
    name: string;
    fullName: string;
    nameEn?: string;
}

export interface Ward {
    code: number;
    name: string;
    provinceCode: number;
}

export interface DepartmentLookup {
    departmentId: string;
    departmentCode: string;
    departmentName: string;
    employeeCount: number;
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
     * Lấy danh sách phường/xã theo mã tỉnh 
     */
    getWards(provinceCode: number): Observable<Ward[]> {
        return this.http.get<Ward[]>(`${this.apiUrl}/master-data/provinces/${provinceCode}/wards`);
    }

    /**
     * Lấy danh sách phòng ban/phân xưởng để gắn chiến dịch tuyển dụng
     */
    getDepartments(): Observable<DepartmentLookup[]> {
        return this.http.get<DepartmentLookup[]>(`${this.apiUrl}/master-data/departments`);
    }
}

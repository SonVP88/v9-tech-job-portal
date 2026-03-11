import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface EmployeeDto {
  userId: string;
  fullName: string;
  email: string;
  phone: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  // Audit Trail
  lockedAt?: string;
  lockedByName?: string;
  lockReason?: string;
}

export interface CreateEmployeeRequest {
  fullName: string;
  email: string;
  phoneNumber: string;
  role: 'HR' | 'INTERVIEWER';
}

export interface ApiResponse<T> {
  success?: boolean;
  data?: T;
  message?: string;
}

@Injectable({
  providedIn: 'root',
})
export class EmployeeService {
  private apiUrl = '/api/employees';

  constructor(private http: HttpClient) { }

  /**
   * Lấy danh sách nhân viên (HR và INTERVIEWER)
   */
  getEmployees(): Observable<EmployeeDto[]> {
    return this.http.get<EmployeeDto[]>(this.apiUrl);
  }

  /**
   * Tạo nhân viên mới
   */
  createEmployee(request: CreateEmployeeRequest): Observable<EmployeeDto> {
    return this.http.post<EmployeeDto>(this.apiUrl, request);
  }

  deactivateEmployee(userId: string, reason?: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/${userId}/deactivate`, { reason });
  }

  reactivateEmployee(userId: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/${userId}/reactivate`, {});
  }

  updateEmployee(userId: string, request: CreateEmployeeRequest): Observable<EmployeeDto> {
    return this.http.put<EmployeeDto>(`${this.apiUrl}/${userId}`, request);
  }

  /**
   * Kiểm tra lịch phỏng vấn chưa thực hiện của Interviewer (dùng trước khi khóa)
   */
  getPendingInterviews(userId: string): Observable<{ count: number; interviews: any[] }> {
    return this.http.get<{ count: number; interviews: any[] }>(`${this.apiUrl}/${userId}/pending-interviews`);
  }
}

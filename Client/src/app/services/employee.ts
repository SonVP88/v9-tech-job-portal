import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface EmployeeDto {
  userId: string;
  fullName: string;
  email: string;
  phone: string;
  role: string;
  roleName?: string;
  isActive: boolean;
  createdAt: string;

  // Organization & Hierarchy
  departmentId?: string;
  departmentCode?: string;
  departmentName?: string;
  managerId?: string;
  managerName?: string;
  subordinatesCount: number;

  // Audit Trail
  lockedAt?: string;
  lockedByName?: string;
  lockReason?: string;
}

export interface DepartmentLookupDto {
  departmentId: string;
  departmentCode: string;
  departmentName: string;
  employeeCount: number;
}

export interface ManagerLookupDto {
  userId: string;
  fullName: string;
  email: string;
  role: string;
  departmentName?: string;
}

export interface CreateEmployeeRequest {
  fullName: string;
  email: string;
  phoneNumber: string;
  role: 'ADMIN' | 'MANAGER' | 'HR' | 'INTERVIEWER';
  departmentId?: string;
  managerId?: string;
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
   * Lấy danh sách nhân viên (ADMIN, MANAGER, HR, INTERVIEWER)
   */
  getEmployees(): Observable<EmployeeDto[]> {
    return this.http.get<EmployeeDto[]>(this.apiUrl);
  }

  /**
   * Lấy danh mục phòng ban
   */
  getDepartments(): Observable<DepartmentLookupDto[]> {
    return this.http.get<DepartmentLookupDto[]>(`${this.apiUrl}/departments`);
  }

  /**
   * Lấy danh sách quản lý / trưởng phòng
   */
  getManagers(): Observable<ManagerLookupDto[]> {
    return this.http.get<ManagerLookupDto[]>(`${this.apiUrl}/managers`);
  }

  /**
   * Tạo nhân viên mới
   */
  createEmployee(request: CreateEmployeeRequest): Observable<EmployeeDto> {
    return this.http.post<EmployeeDto>(this.apiUrl, request);
  }

  toggleStatus(userId: string, reason?: string): Observable<any> {
    return this.http.patch(`/api/users/${userId}/toggle-status`, { reason });
  }

  deactivateEmployee(userId: string, reason?: string): Observable<any> {
    return this.toggleStatus(userId, reason);
  }

  reactivateEmployee(userId: string): Observable<any> {
    return this.toggleStatus(userId);
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

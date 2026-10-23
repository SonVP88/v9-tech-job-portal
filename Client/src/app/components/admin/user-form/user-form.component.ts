import { Component, OnInit, inject, signal, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../../services/auth.service';
import { ToastService } from '../../../services/toast.service';

export interface CurrentUser {
  userId: string;
  email: string;
  name: string;
  role: string;
  departmentId?: string;
}

export interface RoleOption {
  code: string;
  name: string;
}

export interface DepartmentOption {
  departmentId: string;
  departmentName: string;
  departmentCode?: string;
}

@Component({
  selector: 'app-user-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4 animate-fade-in">
      <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-2xl w-full max-w-xl border border-gray-200 dark:border-slate-800 overflow-hidden">
        
        <!-- Header -->
        <div class="px-6 py-4 border-b border-gray-100 dark:border-slate-800 flex justify-between items-center bg-slate-50/50 dark:bg-slate-800/40">
          <div>
            <h3 class="text-lg font-bold text-slate-900 dark:text-white">Thêm Nhân Sự Mới</h3>
            <p class="text-xs text-slate-500 mt-0.5">
              Ủy quyền phân cấp: 
              <span class="font-semibold text-blue-600 dark:text-blue-400">{{ currentUser()?.role }}</span>
            </p>
          </div>
          <button (click)="close.emit()" class="text-gray-400 hover:text-gray-600 dark:hover:text-white">
            <span class="material-symbols-outlined">close</span>
          </button>
        </div>

        <!-- Form Body -->
        <form [formGroup]="userForm" (ngSubmit)="onSubmit()" class="p-6 space-y-4">
          
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                Họ và Tên <span class="text-red-500">*</span>
              </label>
              <input type="text" formControlName="fullName" placeholder="Nguyễn Văn A"
                class="w-full px-3.5 py-2.5 rounded-xl border border-gray-300 dark:border-slate-700 dark:bg-slate-800 text-sm focus:ring-2 focus:ring-blue-500 outline-none" />
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                Email Công Việc <span class="text-red-500">*</span>
              </label>
              <input type="email" formControlName="email" placeholder="user@factory.com"
                class="w-full px-3.5 py-2.5 rounded-xl border border-gray-300 dark:border-slate-700 dark:bg-slate-800 text-sm focus:ring-2 focus:ring-blue-500 outline-none" />
            </div>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                Số Điện Thoại <span class="text-red-500">*</span>
              </label>
              <input type="text" formControlName="phoneNumber" placeholder="0901234567"
                class="w-full px-3.5 py-2.5 rounded-xl border border-gray-300 dark:border-slate-700 dark:bg-slate-800 text-sm focus:ring-2 focus:ring-blue-500 outline-none" />
            </div>

            <div>
              <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
                Mật Khẩu Ban Đầu <span class="text-red-500">*</span>
              </label>
              <input type="password" formControlName="password" placeholder="••••••••"
                class="w-full px-3.5 py-2.5 rounded-xl border border-gray-300 dark:border-slate-700 dark:bg-slate-800 text-sm focus:ring-2 focus:ring-blue-500 outline-none" />
            </div>
          </div>

          <!-- Dropdown Phòng Ban (Delegated Control) -->
          <div>
            <div class="flex justify-between items-center mb-1">
              <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300">
                Phòng Ban / Phân Xưởng <span class="text-red-500">*</span>
              </label>
              <span *ngIf="isManager()" class="text-[11px] text-amber-600 dark:text-amber-400 font-medium flex items-center gap-1">
                <span class="material-symbols-outlined text-[14px]">lock</span>
                Cố định theo phòng ban của bạn
              </span>
            </div>
            <select formControlName="departmentId"
              [class.bg-gray-100]="userForm.get('departmentId')?.disabled"
              [class.cursor-not-allowed]="userForm.get('departmentId')?.disabled"
              class="w-full px-3.5 py-2.5 rounded-xl border border-gray-300 dark:border-slate-700 dark:bg-slate-800 text-sm focus:ring-2 focus:ring-blue-500 outline-none">
              <option value="">-- Chọn phòng ban --</option>
              <option *ngFor="let dept of departments" [value]="dept.departmentId">
                {{ dept.departmentName }}
              </option>
            </select>
          </div>

          <!-- Dropdown Vai Trò (Lọc động theo cấp ủy quyền) -->
          <div>
            <label class="block text-xs font-semibold text-slate-700 dark:text-slate-300 mb-1">
              Vai Trò Bổ Nhiệm <span class="text-red-500">*</span>
            </label>
            <select formControlName="role"
              class="w-full px-3.5 py-2.5 rounded-xl border border-gray-300 dark:border-slate-700 dark:bg-slate-800 text-sm focus:ring-2 focus:ring-blue-500 outline-none">
              <option value="">-- Chọn vai trò --</option>
              <option *ngFor="let r of filteredRoles()" [value]="r.code">
                {{ r.name }}
              </option>
            </select>
            <p *ngIf="isManager()" class="text-[11px] text-slate-500 mt-1">
              * Theo quy tắc ủy quyền, Trưởng phòng chỉ được bổ nhiệm Tổ Trưởng hoặc Nhân Viên (HR/Interviewer).
            </p>
          </div>

          <!-- Actions -->
          <div class="pt-4 flex justify-end gap-3 border-t border-gray-100 dark:border-slate-800">
            <button type="button" (click)="close.emit()"
              class="px-5 py-2.5 rounded-xl border border-gray-200 dark:border-slate-700 text-sm font-semibold hover:bg-gray-50 dark:hover:bg-slate-800 transition-colors">
              Hủy
            </button>
            <button type="submit" [disabled]="userForm.invalid || isSubmitting()"
              class="px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white text-sm font-bold shadow-lg shadow-blue-500/30 transition-all flex items-center gap-2">
              <span *ngIf="isSubmitting()" class="material-symbols-outlined text-sm animate-spin">progress_activity</span>
              <span>Lưu Tài Khoản</span>
            </button>
          </div>
        </form>

      </div>
    </div>
  `
})
export class UserFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private http = inject(HttpClient);
  private toast = inject(ToastService);

  @Input() departments: DepartmentOption[] = [];
  @Output() close = new EventEmitter<void>();
  @Output() userCreated = new EventEmitter<void>();

  userForm!: FormGroup;
  currentUser = signal<CurrentUser | null>(null);
  filteredRoles = signal<RoleOption[]>([]);
  isSubmitting = signal(false);

  // Danh sách toàn bộ các Role trong hệ thống
  private allRoles: RoleOption[] = [
    { code: 'ADMIN', name: 'Ban Giám Đốc (SuperAdmin)' },
    { code: 'MANAGER', name: 'Trưởng Phòng / Quản Đốc (DepartmentManager)' },
    { code: 'HR', name: 'Chuyên Viên Nhân Sự (HR / Staff)' },
    { code: 'INTERVIEWER', name: 'Hội Đồng Phỏng Vấn (TeamLeader / Interviewer)' }
  ];

  isManager(): boolean {
    const role = this.currentUser()?.role;
    return role === 'MANAGER' || role === 'DepartmentManager';
  }

  isSuperAdmin(): boolean {
    const role = this.currentUser()?.role;
    return role === 'ADMIN' || role === 'SuperAdmin';
  }

  ngOnInit(): void {
    const user = this.authService.getCurrentUser();
    this.currentUser.set(user);
    this.initForm();
    this.setupDelegatedFormLogic(user);
  }

  private initForm(): void {
    this.userForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', [Validators.required, Validators.pattern(/(84|0[3|5|7|8|9])+([0-9]{8})\b/)]],
      password: ['Password123!', [Validators.required, Validators.minLength(6)]],
      departmentId: ['', [Validators.required]],
      role: ['HR', [Validators.required]]
    });
  }

  private setupDelegatedFormLogic(user: CurrentUser | null): void {
    if (!user) return;

    if (this.isSuperAdmin()) {
      // SuperAdmin: Enable dropdown phòng ban và hiển thị toàn bộ roles
      this.userForm.get('departmentId')?.enable();
      this.filteredRoles.set(this.allRoles);
    } else if (this.isManager()) {
      // DepartmentManager:
      // a. Gọi .disable() cho control departmentId và gán cứng DepartmentId của CurrentUser
      const deptCtrl = this.userForm.get('departmentId');
      deptCtrl?.setValue(user.departmentId || '');
      deptCtrl?.disable();

      // b. Dùng .filter() loại bỏ option SuperAdmin và DepartmentManager ra khỏi mảng hiển thị
      const allowedRoles = this.allRoles.filter(
        r => r.code !== 'ADMIN' && r.code !== 'MANAGER' && r.code !== 'SuperAdmin' && r.code !== 'DepartmentManager'
      );
      this.filteredRoles.set(allowedRoles);

      this.userForm.patchValue({ role: 'HR' });
    }
  }

  onSubmit(): void {
    if (this.userForm.invalid) {
      this.userForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);

    // Dùng getRawValue() để lấy cả departmentId đã bị .disable()
    const rawVal = this.userForm.getRawValue();

    this.http.post('/api/employees', rawVal).subscribe({
      next: () => {
        this.toast.success('Thành công', 'Đã khởi tạo tài khoản nhân sự mới.');
        this.isSubmitting.set(false);
        this.userCreated.emit();
        this.close.emit();
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.toast.error('Lỗi ủy quyền', err.error?.message || 'Không thể tạo tài khoản.');
      }
    });
  }
}

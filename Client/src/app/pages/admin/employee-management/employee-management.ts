import { Component, OnInit, inject, signal, computed, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { EmployeeService, EmployeeDto, CreateEmployeeRequest, DepartmentLookupDto, ManagerLookupDto } from '../../../services/employee';
import { ToastService } from '../../../services/toast.service';
import { PopupService } from '../../../services/popup.service';
import { AuthService } from '../../../services/auth.service';

export interface DepartmentGroup {
  departmentCode: string;
  departmentName: string;
  manager?: EmployeeDto;
  members: EmployeeDto[];
}

export interface ManagerGroup {
  manager: EmployeeDto;
  subordinates: EmployeeDto[];
}

export interface EmployeeTreeNode {
  employee: EmployeeDto;
  level: number;
  children: EmployeeTreeNode[];
  isExpanded: boolean;
  hasChildren: boolean;
  directSubordinates: EmployeeDto[];
}

export interface FlattenedTreeRow {
  node: EmployeeTreeNode;
  level: number;
  isExpanded: boolean;
  hasChildren: boolean;
}

@Component({
  selector: 'app-employee-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './employee-management.html',
  styleUrl: './employee-management.scss',
})
export class EmployeeManagement implements OnInit {
  private employeeService = inject(EmployeeService);
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);
  private cdr = inject(ChangeDetectorRef);
  private router = inject(Router);
  private toast = inject(ToastService);
  private popup = inject(PopupService);

  // State Signals
  employees = signal<EmployeeDto[]>([]);
  departments = signal<DepartmentLookupDto[]>([]);
  managers = signal<ManagerLookupDto[]>([]);
  isLoading = signal(false);
  showModal = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  // Current Logged-in User Profile
  currentUser = signal<{
    userId: string;
    email: string;
    name: string;
    role: string;
    departmentId?: string;
    departmentCode?: string;
    departmentName?: string;
  } | null>(null);

  // Set lưu trữ các Node ID đang được mở (Expand) trong TreeGrid
  expandedNodeIds = signal<Set<string>>(new Set());

  // View modes: 'table' (TreeGrid phân cấp), 'hierarchy' (Sơ đồ thẻ), 'department' (Theo phòng ban)
  viewMode = signal<'table' | 'hierarchy' | 'department'>('table');

  // Pagination cho Tree (Root branches)
  currentPage = signal(1);
  itemsPerPage = 10;

  // Edit employee
  selectedEmployee = signal<EmployeeDto | null>(null);
  showEditModal = signal(false);

  employeeForm!: FormGroup;
  Math = Math;

  // Search & Filter
  searchQuery = '';
  showFilterPanel = false;
  selectedDeptCode = signal<string>('ALL');
  filterRole = '';
  filterStatus = '';
  filterManagerId = '';

  // Smart Role-Based Flags
  isSuperAdmin = computed(() => this.currentUser()?.role === 'ADMIN');
  isDepartmentManager = computed(() => this.currentUser()?.role === 'MANAGER');
  currentUserDepartmentName = computed(() => this.currentUser()?.departmentName || '');
  currentUserDepartmentCode = computed(() => this.currentUser()?.departmentCode || '');

  // Scope: Nếu là Trưởng phòng, chỉ thống kê và hiển thị trong phạm vi phòng ban của họ
  scopedEmployees = computed(() => {
    const all = this.employees();
    if (this.isDepartmentManager() && this.currentUserDepartmentCode()) {
      return all.filter(e => e.departmentCode === this.currentUserDepartmentCode());
    }
    return all;
  });

  // KPI Statistics (Tự động tính toán lại theo Signal Computed)
  totalEmployees = computed(() => this.scopedEmployees().length);
  totalDirectors = computed(() => this.scopedEmployees().filter(e => e.role === 'ADMIN').length);
  totalManagers = computed(() => this.scopedEmployees().filter(e => e.role === 'MANAGER').length);
  totalHr = computed(() => this.scopedEmployees().filter(e => e.role === 'HR').length);
  totalInterviewers = computed(() => this.scopedEmployees().filter(e => e.role === 'INTERVIEWER').length);
  totalActiveDepartments = computed(() => {
    if (this.isDepartmentManager()) return 1;
    return this.departments().length;
  });

  // Danh sách Tab phòng ban được phép hiển thị cho role hiện tại
  visibleDepartments = computed(() => {
    const depts = this.departments();
    if (this.isDepartmentManager() && this.currentUserDepartmentCode()) {
      return depts.filter(d => d.departmentCode === this.currentUserDepartmentCode());
    }
    return depts;
  });

  /**
   * 1. Thuật toán đệ quy chuyển Flat Array -> Tree Structure (Matrix Tree)
   */
  treeNodes = computed<EmployeeTreeNode[]>(() => {
    const list = this.filteredEmployees();
    const map = new Map<string, EmployeeTreeNode>();
    const allSubsMap = new Map<string, EmployeeDto[]>();

    // Bước 1: Tạo map các TreeNode
    list.forEach(emp => {
      map.set(emp.userId, {
        employee: emp,
        level: 0,
        children: [],
        isExpanded: this.expandedNodeIds().has(emp.userId),
        hasChildren: false,
        directSubordinates: []
      });

      if (emp.managerId) {
        if (!allSubsMap.has(emp.managerId)) {
          allSubsMap.set(emp.managerId, []);
        }
        allSubsMap.get(emp.managerId)!.push(emp);
      }
    });

    // Bước 2: Gán danh sách subordinates trực tiếp cho từng node (dùng cho Avatar Group)
    map.forEach((node, userId) => {
      node.directSubordinates = allSubsMap.get(userId) || [];
    });

    const roots: EmployeeTreeNode[] = [];

    // Bước 3: Ghép mối quan hệ Cha - Con (Hierarchy Tree)
    list.forEach(emp => {
      const node = map.get(emp.userId)!;
      if (emp.managerId && map.has(emp.managerId)) {
        const parent = map.get(emp.managerId)!;
        parent.children.push(node);
        parent.hasChildren = true;
      } else {
        // Cấp cao nhất trong tập dữ liệu lọc (Giám đốc, hoặc nhân sự không có sếp trong danh sách này)
        roots.push(node);
      }
    });

    // Bước 4: Đệ quy gán level theo độ sâu của cây
    const assignLevel = (node: EmployeeTreeNode, lvl: number) => {
      node.level = lvl;
      node.children.forEach(child => assignLevel(child, lvl + 1));
    };
    roots.forEach(r => assignLevel(r, 0));

    return roots;
  });

  /**
   * 2. Đệ quy làm phẳng cây (Flatten) để render trên HTML Table với thụt lề cấp bậc
   */
  flattenedTreeRows = computed<FlattenedTreeRow[]>(() => {
    const roots = this.treeNodes();
    const rows: FlattenedTreeRow[] = [];

    const traverse = (node: EmployeeTreeNode) => {
      rows.push({
        node,
        level: node.level,
        isExpanded: node.isExpanded,
        hasChildren: node.hasChildren
      });

      // Nếu node đang mở (Expanded) -> tiếp tục đệ quy con
      if (node.isExpanded && node.children.length > 0) {
        node.children.forEach(child => traverse(child));
      }
    };

    roots.forEach(r => traverse(r));
    return rows;
  });

  totalPages = (): number => Math.ceil(this.flattenedTreeRows().length / this.itemsPerPage);

  paginatedEmployees = (): FlattenedTreeRow[] => {
    const start = (this.currentPage() - 1) * this.itemsPerPage;
    return this.flattenedTreeRows().slice(start, start + this.itemsPerPage);
  };

  ngOnInit(): void {
    this.initForm();
    this.loadAllData();
  }

  /**
   * Khởi tạo Reactive Form
   */
  private initForm(): void {
    this.employeeForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', [Validators.required, Validators.pattern(/(84|0[3|5|7|8|9])+([0-9]{8})\b/)]],
      role: ['HR', [Validators.required]],
      departmentId: [''],
      managerId: ['']
    });
  }

  /**
   * Load tất cả dữ liệu (Employees, Departments, Managers)
   */
  loadAllData(): void {
    this.loadEmployees();
    this.loadDepartments();
    this.loadManagers();
  }

  loadEmployees(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.employeeService.getEmployees().subscribe({
      next: (data) => {
        this.employees.set(data);

        // 1. Nhận diện User đang đăng nhập để kích hoạt Smart Role-Based UI
        const authUser = this.authService.getCurrentUser();
        if (authUser) {
          const myEmp = data.find(e => 
            e.userId === authUser.userId || 
            (e.email && authUser.email && e.email.toLowerCase() === authUser.email.toLowerCase())
          );

          this.currentUser.set({
            userId: authUser.userId,
            email: authUser.email,
            name: authUser.name,
            role: authUser.role,
            departmentId: myEmp?.departmentId,
            departmentCode: myEmp?.departmentCode,
            departmentName: myEmp?.departmentName
          });

          // Nếu là Trưởng phòng -> Tự động lock và chọn theo phòng ban của họ
          if (authUser.role === 'MANAGER' && myEmp?.departmentCode) {
            this.selectedDeptCode.set(myEmp.departmentCode);
          }
        }

        // 2. Mặc định mở rộng (Expand) các cấp Quản lý & Giám đốc
        const defaultExpanded = new Set<string>();
        data.forEach(e => {
          if (e.role === 'ADMIN' || e.role === 'MANAGER' || e.subordinatesCount > 0) {
            defaultExpanded.add(e.userId);
          }
        });
        this.expandedNodeIds.set(defaultExpanded);

        this.isLoading.set(false);
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.errorMessage.set('Không thể tải danh sách nhân viên. Vui lòng thử lại.');
        this.toast.error('Lỗi', error?.error?.message || 'Không thể tải danh sách nhân viên.');
        this.isLoading.set(false);
        this.cdr.detectChanges();
      }
    });
  }

  loadDepartments(): void {
    this.employeeService.getDepartments().subscribe({
      next: (data) => {
        this.departments.set(data);
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error loading departments', err)
    });
  }

  loadManagers(): void {
    this.employeeService.getManagers().subscribe({
      next: (data) => {
        this.managers.set(data);
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error loading managers', err)
    });
  }

  /**
   * Filter tab theo phòng ban
   */
  selectDepartment(code: string): void {
    this.selectedDeptCode.set(code);
    this.currentPage.set(1);
    this.cdr.detectChanges();
  }

  /**
   * Switch View Mode (Table / Hierarchy / Department)
   */
  setViewMode(mode: 'table' | 'hierarchy' | 'department'): void {
    this.viewMode.set(mode);
    this.cdr.detectChanges();
  }

  /**
   * Filter nhanh cấp dưới của một quản lý
   */
  filterSubordinatesOf(managerId: string, managerName: string): void {
    this.viewMode.set('table');
    this.filterManagerId = managerId;
    this.currentPage.set(1);
    this.toast.info('Bộ lọc cấp dưới', `Đang hiển thị nhân sự báo cáo cho: ${managerName}`);
    this.cdr.detectChanges();
  }

  /**
   * Mở modal thêm nhân viên
   */
  openAddModal(): void {
    const current = this.currentUser();
    if (this.isDepartmentManager() && current?.departmentId) {
      this.employeeForm.reset({
        role: 'HR',
        departmentId: current.departmentId,
        managerId: current.userId
      });
      this.employeeForm.get('departmentId')?.disable();
    } else {
      this.employeeForm.reset({
        role: 'HR',
        departmentId: '',
        managerId: ''
      });
      this.employeeForm.get('departmentId')?.enable();
    }
    this.showModal.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);
  }

  /**
   * Đóng modal
   */
  closeModal(): void {
    this.showModal.set(false);
    this.employeeForm.reset();
    this.errorMessage.set(null);
    this.successMessage.set(null);
  }

  /**
   * Submit form tạo nhân viên mới
   */
  onSubmit(): void {
    if (this.employeeForm.invalid) {
      Object.keys(this.employeeForm.controls).forEach(key => {
        this.employeeForm.get(key)?.markAsTouched();
      });
      this.toast.warning('Thiếu thông tin', 'Vui lòng kiểm tra và điền đầy đủ các thông tin bắt buộc');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    // Sử dụng getRawValue() để lấy cả giá trị của control bị disable (departmentId)
    const val = this.employeeForm.getRawValue();
    const request: CreateEmployeeRequest = {
      fullName: val.fullName,
      email: val.email,
      phoneNumber: val.phoneNumber,
      role: val.role,
      departmentId: val.departmentId ? val.departmentId : undefined,
      managerId: val.managerId ? val.managerId : undefined
    };

    this.employeeService.createEmployee(request).subscribe({
      next: (newEmployee) => {
        this.isLoading.set(false);
        this.successMessage.set(' Tạo nhân viên thành công!');
        this.toast.success('Thành công', 'Tạo nhân viên thành công!');

        setTimeout(() => {
          this.closeModal();
          this.loadAllData();
        }, 1200);

        this.cdr.detectChanges();
      },
      error: (error) => {
        const errorMessage = error?.error?.message || 'Không thể tạo nhân viên. Vui lòng thử lại.';
        this.errorMessage.set(errorMessage);
        this.toast.error('Lỗi', errorMessage);
        this.isLoading.set(false);
        this.cdr.detectChanges();
      }
    });
  }

  /**
   * View/Edit employee - Open modal with form populated
   */
  viewEmployee(employee: EmployeeDto): void {
    this.selectedEmployee.set(employee);
    this.employeeForm.patchValue({
      fullName: employee.fullName,
      email: employee.email,
      phoneNumber: employee.phone,
      role: employee.role,
      departmentId: employee.departmentId || '',
      managerId: employee.managerId || ''
    });
    this.showEditModal.set(true);
  }

  closeEditModal(): void {
    this.showEditModal.set(false);
    this.selectedEmployee.set(null);
    this.employeeForm.reset({ role: 'HR', departmentId: '', managerId: '' });
  }

  /**
   * Update employee info
   */
  updateEmployee(): void {
    if (this.employeeForm.invalid || !this.selectedEmployee()) {
      Object.keys(this.employeeForm.controls).forEach(key => {
        this.employeeForm.get(key)?.markAsTouched();
      });
      this.toast.warning('Thiếu thông tin', 'Vui lòng điền đúng và đủ thông tin nhân viên');
      return;
    }

    this.isLoading.set(true);
    const val = this.employeeForm.value;
    const request: CreateEmployeeRequest = {
      fullName: val.fullName,
      email: val.email,
      phoneNumber: val.phoneNumber,
      role: val.role,
      departmentId: val.departmentId ? val.departmentId : undefined,
      managerId: val.managerId ? val.managerId : undefined
    };
    const userId = this.selectedEmployee()!.userId;

    this.employeeService.updateEmployee(userId, request).subscribe({
      next: (updated) => {
        this.successMessage.set(' Cập nhật nhân viên thành công!');
        this.toast.success('Thành công', 'Cập nhật nhân viên thành công!');

        setTimeout(() => {
          this.closeEditModal();
          this.loadAllData();
          this.successMessage.set(null);
        }, 1200);

        this.isLoading.set(false);
        this.cdr.detectChanges();
      },
      error: (error) => {
        const errorMessage = error?.error?.message || 'Không thể cập nhật nhân viên. Vui lòng thử lại.';
        this.errorMessage.set(errorMessage);
        this.toast.error('Cập nhật thất bại', errorMessage);
        this.isLoading.set(false);
        this.cdr.detectChanges();
      }
    });
  }

  /**
   * Toggle Switch: Khóa / Mở khóa tài khoản nhân sự (Xóa mềm)
   */
  async toggleEmployeeStatus(employee: EmployeeDto): Promise<void> {
    const actionText = employee.isActive ? 'Khóa' : 'Mở khóa';

    const confirmed = await this.popup.confirm({
      title: `${actionText} tài khoản`,
      message: `Bạn có chắc chắn muốn ${actionText.toLowerCase()} tài khoản của nhân viên "${employee.fullName}"?`,
      confirmText: actionText,
      cancelText: 'Hủy',
      tone: employee.isActive ? 'danger' : 'primary'
    });

    if (!confirmed) return;

    let reason: string | undefined = undefined;
    if (employee.isActive) {
      const reasonInput = await this.popup.prompt({
        title: 'Lý do khóa tài khoản',
        message: 'Nhập lý do khóa tài khoản (có thể bỏ trống):',
        placeholder: 'Nhập lý do...',
        confirmText: 'Khóa ngay',
        cancelText: 'Bỏ qua',
        tone: 'neutral',
      });
      reason = reasonInput === null ? undefined : reasonInput;
    }

    this.isLoading.set(true);
    this.employeeService.toggleStatus(employee.userId, reason).subscribe({
      next: (res) => {
        employee.isActive = res.isActive;
        this.toast.success('Thành công', res.message || `${actionText} tài khoản thành công!`);
        this.loadAllData();
        this.isLoading.set(false);
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.toast.error('Lỗi ủy quyền', err.error?.message || `Không thể ${actionText.toLowerCase()} tài khoản.`);
        this.isLoading.set(false);
        this.cdr.detectChanges();
      }
    });
  }

  /**
   * Kiểm tra xem người dùng hiện tại có quyền toggle tài khoản này không:
   * - Không được tự khóa chính mình
   * - Trưởng phòng không được khóa Trưởng phòng khác hoặc Ban Giám Đốc
   */
  isToggleDisabled(employee: EmployeeDto): boolean {
    const current = this.currentUser();
    if (!current) return true;

    // 1. Không được tự khóa chính mình
    if (employee.userId === current.userId) return true;

    // 2. SuperAdmin có quyền với tất cả (trừ chính mình)
    if (this.isSuperAdmin()) return false;

    // 3. Manager chỉ được thao tác với nhân sự cùng phòng ban và cấp dưới
    if (this.isDepartmentManager()) {
      const isDifferentDept = employee.departmentId !== current.departmentId;
      const isManagerOrAdmin = employee.role === 'ADMIN' || employee.role === 'MANAGER';
      return isDifferentDept || isManagerOrAdmin;
    }

    return true;
  }

  /**
   * Reactivate employee
   */
  async reactivateEmployee(userId: string): Promise<void> {
    const confirmed = await this.popup.confirm({
      title: 'Kích hoạt lại tài khoản',
      message: 'Bạn có chắc chắn muốn kích hoạt lại nhân viên này?',
      confirmText: 'Kích hoạt',
      cancelText: 'Hủy',
      tone: 'primary',
    });
    if (!confirmed) return;

    this.isLoading.set(true);
    this.employeeService.reactivateEmployee(userId).subscribe({
      next: () => {
        this.toast.success('Thành công', 'Kích hoạt nhân viên thành công!');
        setTimeout(() => this.loadAllData(), 1000);
        this.isLoading.set(false);
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.toast.error('Lỗi', 'Không thể kích hoạt nhân viên.');
        this.isLoading.set(false);
        this.cdr.detectChanges();
      }
    });
  }

  /**
   * Delete / Deactivate employee
   */
  async deleteEmployee(userId: string, employeeName: string, employeeRole: string): Promise<void> {
    this.isLoading.set(true);

    this.employeeService.getPendingInterviews(userId).subscribe({
      next: async (data) => {
        this.isLoading.set(false);
        let confirmMessage = `Bạn có chắc muốn khóa tài khoản "${employeeName}" (${employeeRole})?`;

        if (employeeRole === 'INTERVIEWER' && data.count > 0) {
          confirmMessage = `⚠️ CẢNH BÁO: "${employeeName}" đang có ${data.count} lịch phỏng vấn chưa hoàn tất. Bạn vẫn muốn khóa tài khoản này?`;
        }

        const confirmed = await this.popup.confirm({
          title: 'Xác nhận khóa tài khoản',
          message: confirmMessage,
          confirmText: 'Khóa tài khoản',
          cancelText: 'Hủy',
          tone: 'danger',
        });
        if (!confirmed) return;

        const reasonInput = await this.popup.prompt({
          title: 'Lý do khóa tài khoản',
          message: 'Nhập lý do khóa tài khoản (có thể bỏ trống):',
          placeholder: 'Nhập lý do...',
          confirmText: 'Khóa ngay',
          cancelText: 'Bỏ qua',
          tone: 'neutral',
        });
        const reason = reasonInput === null ? undefined : reasonInput;

        this.isLoading.set(true);
        this.employeeService.deactivateEmployee(userId, reason).subscribe({
          next: () => {
            this.toast.success('Thành công', 'Đã khóa tài khoản nhân viên!');
            setTimeout(() => this.loadAllData(), 1200);
            this.isLoading.set(false);
            this.cdr.detectChanges();
          },
          error: (error) => {
            this.toast.error('Lỗi', error?.error?.message || 'Không thể khóa nhân viên.');
            this.isLoading.set(false);
            this.cdr.detectChanges();
          }
        });
      },
      error: async () => {
        this.isLoading.set(false);
        const confirmed = await this.popup.confirm({
          title: 'Khóa tài khoản',
          message: `Bạn có chắc muốn khóa tài khoản "${employeeName}"?`,
          confirmText: 'Khóa tài khoản',
          cancelText: 'Hủy',
          tone: 'danger',
        });
        if (!confirmed) return;
        this.isLoading.set(true);
        this.employeeService.deactivateEmployee(userId).subscribe({
          next: () => {
            this.toast.success('Thành công', 'Đã khóa tài khoản nhân viên!');
            setTimeout(() => this.loadAllData(), 1000);
            this.isLoading.set(false);
          },
          error: (error) => {
            this.toast.error('Lỗi', error?.error?.message || 'Không thể khóa nhân viên.');
            this.isLoading.set(false);
          }
        });
      }
    });
  }

  // ==================== FILTER & SEARCH ====================
  filteredEmployees(): EmployeeDto[] {
    let filtered = [...this.scopedEmployees()];

    // Filter theo Tab phòng ban (nếu là SuperAdmin)
    if (!this.isDepartmentManager() && this.selectedDeptCode() !== 'ALL') {
      filtered = filtered.filter(emp => emp.departmentCode === this.selectedDeptCode());
    }

    // Filter theo Quản lý trực tiếp (nếu có click từ chip filter)
    if (this.filterManagerId) {
      filtered = filtered.filter(emp => emp.managerId === this.filterManagerId);
    }

    // Search query
    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase().trim();
      filtered = filtered.filter(emp =>
        emp.fullName.toLowerCase().includes(q) ||
        emp.email.toLowerCase().includes(q) ||
        (emp.phone && emp.phone.includes(q)) ||
        (emp.departmentName && emp.departmentName.toLowerCase().includes(q))
      );
    }

    // Filter Role
    if (this.filterRole) {
      filtered = filtered.filter(emp => emp.role === this.filterRole);
    }

    // Filter Status
    if (this.filterStatus) {
      const isActive = this.filterStatus === 'active';
      filtered = filtered.filter(emp => emp.isActive === isActive);
    }

    return filtered;
  }

  /**
   * Toggle mở / đóng 1 nhánh cây phân cấp
   */
  toggleNode(userId: string): void {
    const current = new Set(this.expandedNodeIds());
    if (current.has(userId)) {
      current.delete(userId);
    } else {
      current.add(userId);
    }
    this.expandedNodeIds.set(current);
  }

  /**
   * Mở rộng tất cả các cấp trong cây
   */
  expandAll(): void {
    const allIds = new Set<string>();
    this.employees().forEach(e => {
      if (e.role === 'ADMIN' || e.role === 'MANAGER' || e.subordinatesCount > 0) {
        allIds.add(e.userId);
      }
    });
    this.expandedNodeIds.set(allIds);
  }

  /**
   * Thu gọn toàn bộ cây
   */
  collapseAll(): void {
    this.expandedNodeIds.set(new Set());
  }

  /**
   * 3. UI Helpers & Color Mapping
   */
  getAvatarGradient(fullName: string): string {
    const gradients = [
      'bg-gradient-to-tr from-blue-600 to-indigo-600 text-white',
      'bg-gradient-to-tr from-purple-600 to-pink-600 text-white',
      'bg-gradient-to-tr from-emerald-600 to-teal-600 text-white',
      'bg-gradient-to-tr from-amber-500 to-orange-600 text-white',
      'bg-gradient-to-tr from-rose-600 to-red-600 text-white',
      'bg-gradient-to-tr from-cyan-600 to-blue-600 text-white',
      'bg-gradient-to-tr from-violet-600 to-indigo-700 text-white'
    ];
    let hash = 0;
    const str = fullName || 'NV';
    for (let i = 0; i < str.length; i++) {
      hash = str.charCodeAt(i) + ((hash << 5) - hash);
    }
    const idx = Math.abs(hash) % gradients.length;
    return gradients[idx];
  }

  getDepartmentBadge(code?: string): { badge: string; dot: string; label: string } {
    switch (code) {
      case 'BGD':
        return {
          badge: 'bg-purple-50 text-purple-700 border-purple-200 dark:bg-purple-950/40 dark:text-purple-300 dark:border-purple-800',
          dot: 'bg-purple-500',
          label: 'Ban Giám Đốc'
        };
      case 'HR':
        return {
          badge: 'bg-sky-50 text-sky-700 border-sky-200 dark:bg-sky-950/40 dark:text-sky-300 dark:border-sky-800',
          dot: 'bg-sky-500',
          label: 'Phòng Nhân Sự'
        };
      case 'IT':
        return {
          badge: 'bg-indigo-50 text-indigo-700 border-indigo-200 dark:bg-indigo-950/40 dark:text-indigo-300 dark:border-indigo-800',
          dot: 'bg-indigo-500',
          label: 'Phòng Công Nghệ TT'
        };
      case 'PROD':
        return {
          badge: 'bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-950/40 dark:text-amber-300 dark:border-amber-800',
          dot: 'bg-amber-500',
          label: 'Phòng Sản Xuất'
        };
      case 'QA':
        return {
          badge: 'bg-teal-50 text-teal-700 border-teal-200 dark:bg-teal-950/40 dark:text-teal-300 dark:border-teal-800',
          dot: 'bg-teal-500',
          label: 'Phòng Tối Ưu & CL'
        };
      case 'SALES':
        return {
          badge: 'bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-950/40 dark:text-emerald-300 dark:border-emerald-800',
          dot: 'bg-emerald-500',
          label: 'Phòng Kinh Doanh'
        };
      case 'FIN':
        return {
          badge: 'bg-rose-50 text-rose-700 border-rose-200 dark:bg-rose-950/40 dark:text-rose-300 dark:border-rose-800',
          dot: 'bg-rose-500',
          label: 'Phòng Kế Toán - TC'
        };
      default:
        return {
          badge: 'bg-gray-50 text-gray-700 border-gray-200 dark:bg-slate-800 dark:text-gray-300 dark:border-slate-700',
          dot: 'bg-gray-400',
          label: 'Chưa phân bổ'
        };
    }
  }

  getRoleBadge(role: string): string {
    switch (role) {
      case 'ADMIN':
        return 'bg-purple-100 text-purple-800 dark:bg-purple-900/60 dark:text-purple-300 border-purple-200 dark:border-purple-800';
      case 'MANAGER':
        return 'bg-indigo-100 text-indigo-800 dark:bg-indigo-900/60 dark:text-indigo-300 border-indigo-200 dark:border-indigo-800';
      case 'HR':
        return 'bg-sky-100 text-sky-800 dark:bg-sky-900/60 dark:text-sky-300 border-sky-200 dark:border-sky-800';
      case 'INTERVIEWER':
        return 'bg-teal-100 text-teal-800 dark:bg-teal-900/60 dark:text-teal-300 border-teal-200 dark:border-teal-800';
      default:
        return 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-300 border-gray-200';
    }
  }

  getLevelBadge(level: number, role: string): { label: string; class: string } {
    if (role === 'ADMIN') {
      return { label: 'Cấp 1 - Giám Đốc', class: 'bg-purple-50 text-purple-700 border-purple-200 dark:bg-purple-950/40 dark:text-purple-300' };
    }
    if (role === 'MANAGER') {
      return { label: 'Cấp 2 - Trưởng Phòng', class: 'bg-indigo-50 text-indigo-700 border-indigo-200 dark:bg-indigo-950/40 dark:text-indigo-300' };
    }
    if (level === 0) {
      return { label: 'Cấp 2 - Quản Lý Độc Lập', class: 'bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-950/40 dark:text-blue-300' };
    }
    return { label: `Cấp ${level + 1} - Chuyên Viên`, class: 'bg-slate-50 text-slate-600 border-slate-200 dark:bg-slate-800 dark:text-slate-400' };
  }

  /**
   * Tính toán nhóm theo Phòng ban cho View Mode 'department'
   */
  getDepartmentGroups(): DepartmentGroup[] {
    const list = this.filteredEmployees();
    const groupsMap = new Map<string, DepartmentGroup>();

    // Init known departments
    for (const d of this.departments()) {
      groupsMap.set(d.departmentCode, {
        departmentCode: d.departmentCode,
        departmentName: d.departmentName,
        members: []
      });
    }

    // Assign members
    for (const emp of list) {
      const code = emp.departmentCode || 'OTHER';
      if (!groupsMap.has(code)) {
        groupsMap.set(code, {
          departmentCode: code,
          departmentName: emp.departmentName || 'Chưa phân bổ',
          members: []
        });
      }
      const grp = groupsMap.get(code)!;
      grp.members.push(emp);
      if (emp.role === 'MANAGER' && !grp.manager) {
        grp.manager = emp;
      }
    }

    // Return only groups that have members or are officially registered
    return Array.from(groupsMap.values()).filter(g => g.members.length > 0);
  }

  /**
   * Tính toán sơ đồ phân cấp Quản lý -> Cấp dưới cho View Mode 'hierarchy'
   */
  getHierarchyGroups(): ManagerGroup[] {
    const all = this.employees();
    const managers = all.filter(e => e.role === 'ADMIN' || e.role === 'MANAGER');
    const result: ManagerGroup[] = [];

    for (const mgr of managers) {
      const subs = all.filter(e => e.managerId === mgr.userId);
      result.push({
        manager: mgr,
        subordinates: subs
      });
    }

    return result;
  }

  toggleFilterPanel(): void {
    this.showFilterPanel = !this.showFilterPanel;
  }

  onSearchChange(): void {
    this.currentPage.set(1);
    this.cdr.detectChanges();
  }

  applyFilters(): void {
    this.currentPage.set(1);
    this.cdr.detectChanges();
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.filterRole = '';
    this.filterStatus = '';
    this.filterManagerId = '';
    this.selectedDeptCode.set('ALL');
    this.currentPage.set(1);
    this.cdr.detectChanges();
  }

  getActiveFiltersCount(): number {
    let count = 0;
    if (this.selectedDeptCode() !== 'ALL') count++;
    if (this.filterRole) count++;
    if (this.filterStatus) count++;
    if (this.filterManagerId) count++;
    return count;
  }

  changePage(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.currentPage.set(page);
    }
  }

  hasError(fieldName: string): boolean {
    const field = this.employeeForm.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }

  getErrorMessage(fieldName: string): string {
    const field = this.employeeForm.get(fieldName);
    if (field?.hasError('required')) return 'Trường này là bắt buộc';
    if (field?.hasError('email')) return 'Email không hợp lệ';
    if (field?.hasError('pattern') && fieldName === 'phoneNumber') return 'Số điện thoại không hợp lệ (VD: 0987654321)';
    if (field?.hasError('minlength')) return `Tối thiểu ${field.errors?.['minlength'].requiredLength} ký tự`;
    return '';
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    if (!dateStr.endsWith('Z') && !dateStr.includes('+')) {
      dateStr += 'Z';
    }
    const date = new Date(dateStr);
    return date.toLocaleDateString('vi-VN', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  getInitials(fullName: string): string {
    const names = (fullName || 'NV').trim().split(' ');
    if (names.length >= 2) {
      return (names[0][0] + names[names.length - 1][0]).toUpperCase();
    }
    return (fullName || 'NV').substring(0, 2).toUpperCase();
  }

  getRoleBadgeClass(role: string): string {
    switch (role) {
      case 'ADMIN':
        return 'bg-purple-100 text-purple-800 dark:bg-purple-900/60 dark:text-purple-300 border border-purple-200 dark:border-purple-800';
      case 'MANAGER':
        return 'bg-indigo-100 text-indigo-800 dark:bg-indigo-900/60 dark:text-indigo-300 border border-indigo-200 dark:border-indigo-800';
      case 'HR':
        return 'bg-blue-100 text-blue-800 dark:bg-blue-900/60 dark:text-blue-300 border border-blue-200 dark:border-blue-800';
      case 'INTERVIEWER':
        return 'bg-teal-100 text-teal-800 dark:bg-teal-900/60 dark:text-teal-300 border border-teal-200 dark:border-teal-800';
      default:
        return 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-300';
    }
  }

  getRoleIcon(role: string): string {
    switch (role) {
      case 'ADMIN': return 'military_tech';
      case 'MANAGER': return 'manage_accounts';
      case 'HR': return 'badge';
      case 'INTERVIEWER': return 'rate_review';
      default: return 'person';
    }
  }

  getDepartmentBadgeClass(code?: string): string {
    switch (code) {
      case 'BGD': return 'bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-950/40 dark:text-amber-300';
      case 'HR': return 'bg-sky-50 text-sky-700 border-sky-200 dark:bg-sky-950/40 dark:text-sky-300';
      case 'IT': return 'bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-950/40 dark:text-blue-300';
      case 'PROD': return 'bg-orange-50 text-orange-700 border-orange-200 dark:bg-orange-950/40 dark:text-orange-300';
      case 'QA': return 'bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-950/40 dark:text-emerald-300';
      case 'SALES': return 'bg-rose-50 text-rose-700 border-rose-200 dark:bg-rose-950/40 dark:text-rose-300';
      case 'FIN': return 'bg-violet-50 text-violet-700 border-violet-200 dark:bg-violet-950/40 dark:text-violet-300';
      default: return 'bg-gray-50 text-gray-600 border-gray-200 dark:bg-slate-800 dark:text-gray-400';
    }
  }

  getAvatarClass(role: string): string {
    switch (role) {
      case 'ADMIN': return 'bg-purple-600 text-white shadow-sm';
      case 'MANAGER': return 'bg-indigo-600 text-white shadow-sm';
      case 'HR': return 'bg-blue-600 text-white shadow-sm';
      case 'INTERVIEWER': return 'bg-teal-600 text-white shadow-sm';
      default: return 'bg-gray-500 text-white';
    }
  }

  exportToExcel(): void {
    const filtered = this.filteredEmployees();
    if (filtered.length === 0) {
      this.toast.warning('Không có dữ liệu', 'Không có dữ liệu để xuất!');
      return;
    }

    const headers = ['Họ tên', 'Email', 'Số điện thoại', 'Vai trò', 'Phòng ban', 'Quản lý trực tiếp', 'Trạng thái', 'Ngày tạo'];
    const rows = filtered.map(emp => [
      emp.fullName,
      `'${emp.email}`,
      emp.phone ? `'${emp.phone}` : '',
      emp.roleName || emp.role,
      emp.departmentName || 'Chưa phân bổ',
      emp.managerName || 'Không có',
      emp.isActive ? 'Hoạt động' : 'Đã khóa',
      this.formatDate(emp.createdAt)
    ]);

    const csvContent = [
      headers.join(','),
      ...rows.map(row => row.map(cell => `"${String(cell).replace(/"/g, '""')}"`).join(','))
    ].join('\n');

    const blob = new Blob(['\uFEFF' + csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = `danh-sach-nhan-su-${new Date().toISOString().split('T')[0]}.csv`;
    link.click();
  }
}

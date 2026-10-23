export type AppRole =
  | 'SuperAdmin'
  | 'DepartmentManager'
  | 'TeamLeader'
  | 'Staff'
  | 'ADMIN'
  | 'MANAGER'
  | 'HR'
  | 'INTERVIEWER';

export type DepartmentCode =
  | 'ADMIN_SYS'   // Ban Giám Đốc / Hệ thống
  | 'HR_TEAM'     // Phòng Nhân Sự
  | 'INT_TEAM'    // Hội Đồng Phỏng Vấn
  | 'PROD_LINE'   // Xưởng Sản Xuất
  | 'WH_LOGS'     // Kho & Logistics
  | 'TECH_MNT'    // Kỹ Thuật & Bảo Trì
  | 'QA_QC';      // Quản Lý Chất Lượng

export interface CurrentUser {
  id: string;
  fullName: string;
  email: string;
  role: AppRole;
  roleDisplayName: string;
  departmentId?: string;
  departmentCode?: DepartmentCode;
  departmentName?: string;
}

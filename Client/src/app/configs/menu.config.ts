import { CurrentUser } from '../models/auth.model';

export type BadgeType = 'ai' | 'counter' | 'warning';

export interface MenuBadge {
  type: BadgeType;
  text?: string;
  badgeKey?: 'openJobs' | 'pendingApplications' | 'todayInterviews';
  cssClass?: string;
}

export interface MenuItem {
  label: string;
  route: string;
  icon: string;
  canAccess: (user: CurrentUser | null) => boolean;
  badge?: MenuBadge;
}

export interface MenuGroup {
  groupTitle: string;
  groupKey: 'DESK' | 'RECRUITMENT' | 'ORG_EVAL' | 'SYSTEM';
  items: MenuItem[];
}

/**
 * ════════════════════════════════════════════════════════════════════════════
 * CẤU HÌNH MENU PHÂN NHÓM DOANH NGHIỆP (GROUPED NAVIGATION)
 * Hệ thống Quản lý Tuyển dụng Nhân sự (ATS) Nhà máy / KCN
 * ════════════════════════════════════════════════════════════════════════════
 */
export const SIDEBAR_MENU_GROUPS: MenuGroup[] = [
  // ─── NHÓM 1 - BÀN LÀM VIỆC ──────────────────────────────────────────────
  {
    groupTitle: 'BÀN LÀM VIỆC',
    groupKey: 'DESK',
    items: [
      {
        label: 'Tổng Quan',
        route: '/hr/dashboard',
        icon: 'dashboard',
        canAccess: () => true
      },
      {
        label: 'Sàng Lọc Hồ Sơ',
        route: '/hr/ai-screening',
        icon: 'fact_check',
        canAccess: (user) => {
          if (!user) return false;
          const isSuper = user.role === 'SuperAdmin' || user.role === 'ADMIN' || user.departmentCode === 'ADMIN_SYS';
          const isHr = user.departmentCode === 'HR_TEAM' || user.role === 'HR';
          return isSuper || isHr;
        },
        badge: {
          type: 'ai',
          text: 'Tự động',
          cssClass: 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300 font-semibold text-[10px] px-2 py-0.5 rounded-md border border-slate-200 dark:border-slate-700'
        }
      }
    ]
  },

  // ─── NHÓM 2 - NGHIỆP VỤ TUYỂN DỤNG ─────────────────────────────────────
  {
    groupTitle: 'NGHIỆP VỤ TUYỂN DỤNG',
    groupKey: 'RECRUITMENT',
    items: [
      {
        label: 'Chiến Dịch Việc Làm',
        route: '/hr/jobs',
        icon: 'work',
        canAccess: (user) => {
          if (!user) return false;
          const isSuper = user.role === 'SuperAdmin' || user.role === 'ADMIN' || user.departmentCode === 'ADMIN_SYS';
          const isHr = user.departmentCode === 'HR_TEAM' || user.role === 'HR';
          const isPlantManager =
            (user.role === 'DepartmentManager' || user.role === 'MANAGER') &&
            ['PROD_LINE', 'WH_LOGS', 'TECH_MNT', 'QA_QC'].includes(user.departmentCode || '');
          return isSuper || isHr || isPlantManager;
        },
        badge: {
          type: 'counter',
          badgeKey: 'openJobs',
          cssClass: 'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300 font-bold text-xs px-2 py-0.5 rounded-full'
        }
      },
      {
        label: 'Hồ Sơ Ứng Tuyển',
        route: '/hr/applications',
        icon: 'assignment_ind',
        canAccess: () => true,
        badge: {
          type: 'counter',
          badgeKey: 'pendingApplications',
          cssClass: 'bg-red-100 text-red-600 dark:bg-red-900/40 dark:text-red-300 font-bold text-xs px-2 py-0.5 rounded-full ring-1 ring-red-200 dark:ring-red-800'
        }
      },
      {
        label: 'Lịch Phỏng Vấn',
        route: '/hr/interviews',
        icon: 'calendar_month',
        canAccess: () => true,
        badge: {
          type: 'counter',
          badgeKey: 'todayInterviews',
          cssClass: 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300 font-bold text-xs px-2 py-0.5 rounded-full'
        }
      },
      {
        label: 'Tiếp Nhận & Onboarding',
        route: '/hr/onboarding',
        icon: 'how_to_reg',
        canAccess: (user) => {
          if (!user) return false;
          const isSuper = user.role === 'SuperAdmin' || user.role === 'ADMIN' || user.departmentCode === 'ADMIN_SYS';
          const isHr = user.departmentCode === 'HR_TEAM' || user.role === 'HR';
          const isManager = user.role === 'DepartmentManager' || user.role === 'MANAGER';
          return isSuper || isHr || isManager;
        }
      },
      {
        label: 'Kho Nhân Tài (Talent Pool)',
        route: '/hr/talent-pool',
        icon: 'folder_shared',
        canAccess: (user) => {
          if (!user) return false;
          const isSuper = user.role === 'SuperAdmin' || user.role === 'ADMIN' || user.departmentCode === 'ADMIN_SYS';
          const isHr = user.departmentCode === 'HR_TEAM' || user.role === 'HR';
          return isSuper || isHr;
        }
      }
    ]
  },

  // ─── NHÓM 3 - TỔ CHỨC & ĐÁNH GIÁ ────────────────────────────────────────
  {
    groupTitle: 'TỔ CHỨC & ĐÁNH GIÁ',
    groupKey: 'ORG_EVAL',
    items: [
      {
        label: 'Cơ Cấu Nhân Sự',
        route: '/hr/employees',
        icon: 'diversity_3',
        canAccess: (user) => {
          if (!user) return false;
          const isSuper = user.role === 'SuperAdmin' || user.role === 'ADMIN' || user.departmentCode === 'ADMIN_SYS';
          const isManager = user.role === 'DepartmentManager' || user.role === 'MANAGER';
          return isSuper || isManager;
        }
      },
      {
        label: 'Tiêu Chuẩn Kỹ Năng',
        route: '/hr/skills',
        icon: 'psychology',
        canAccess: (user) => {
          if (!user) return false;
          const isSuper = user.role === 'SuperAdmin' || user.role === 'ADMIN' || user.departmentCode === 'ADMIN_SYS';
          const isHr = user.departmentCode === 'HR_TEAM' || user.role === 'HR';
          return isSuper || isHr;
        }
      },
      {
        label: 'Ngân Hàng Câu Hỏi',
        route: '/hr/question-bank',
        icon: 'quiz',
        canAccess: () => true
      },
      {
        label: 'Báo Cáo & SLA',
        route: '/hr/reports',
        icon: 'analytics',
        canAccess: (user) => {
          if (!user) return false;
          const isSuper = user.role === 'SuperAdmin' || user.role === 'ADMIN' || user.departmentCode === 'ADMIN_SYS';
          const isManager = user.role === 'DepartmentManager' || user.role === 'MANAGER';
          return isSuper || isManager;
        }
      }
    ]
  },

  // ─── NHÓM 4 - HỆ THỐNG ──────────────────────────────────────────────────
  {
    groupTitle: 'HỆ THỐNG',
    groupKey: 'SYSTEM',
    items: [
      {
        label: 'Huấn Luyện Chatbot FAQ',
        route: '/hr/chatbot-faq',
        icon: 'smart_toy',
        canAccess: (user) => {
          if (!user) return false;
          const isSuper = user.role === 'SuperAdmin' || user.role === 'ADMIN' || user.departmentCode === 'ADMIN_SYS';
          const isHr = user.departmentCode === 'HR_TEAM' || user.role === 'HR';
          return isSuper || isHr;
        }
      },
      {
        label: 'Cài Đặt Hệ Thống',
        route: '/hr/settings',
        icon: 'settings',
        canAccess: (user) => {
          if (!user) return false;
          return user.role === 'SuperAdmin' || user.role === 'ADMIN' || user.departmentCode === 'ADMIN_SYS';
        }
      }
    ]
  }
];

/**
 * Danh sách phẳng tất cả các Menu Items phục vụ Route Guards và xử lý tương thích
 */
export const SIDEBAR_MENU_ITEMS: MenuItem[] = SIDEBAR_MENU_GROUPS.flatMap(group => group.items);

/**
 * 4 Tài khoản mẫu cho bộ chuyển đổi Role nhanh (Dev Role Switcher)
 */
export const DEV_ACCOUNTS: CurrentUser[] = [
  {
    id: '30000000-0000-0000-0000-000000000001',
    fullName: 'Võ Văn Sơn',
    email: 'giamdoc@factory.com',
    role: 'SuperAdmin',
    roleDisplayName: 'Ban Giám Đốc',
    departmentId: '10000000-0000-0000-0000-000000000001',
    departmentCode: 'ADMIN_SYS',
    departmentName: 'Ban Giám Đốc'
  },
  {
    id: '30000000-0000-0000-0000-000000000011',
    fullName: 'Lê Thanh Hương',
    email: 'hr1@factory.com',
    role: 'Staff',
    roleDisplayName: 'Chuyên Viên Tuyển Dụng',
    departmentId: '10000000-0000-0000-0000-000000000002',
    departmentCode: 'HR_TEAM',
    departmentName: 'Phòng Nhân Sự'
  },
  {
    id: '30000000-0000-0000-0000-000000000030',
    fullName: 'Vũ Đức Thắng',
    email: 'truong.sx@factory.com',
    role: 'DepartmentManager',
    roleDisplayName: 'Quản Đốc Sản Xuất',
    departmentId: '10000000-0000-0000-0000-000000000004',
    departmentCode: 'PROD_LINE',
    departmentName: 'Xưởng Sản Xuất'
  },
  {
    id: '30000000-0000-0000-0000-000000000021',
    fullName: 'Trần Minh Quân',
    email: 'pv1@factory.com',
    role: 'Staff',
    roleDisplayName: 'Phỏng Vấn Viên Chuyên Môn',
    departmentId: '10000000-0000-0000-0000-000000000003',
    departmentCode: 'INT_TEAM',
    departmentName: 'Hội Đồng Phỏng Vấn'
  }
];

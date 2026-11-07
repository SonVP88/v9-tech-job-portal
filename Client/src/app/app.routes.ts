import { Routes } from '@angular/router';
import { Login } from './pages/login/login';
import { Register } from './pages/register/register';
import { Home } from './pages/candidate/home/home';
import { JobDetail } from './pages/candidate/job-detail/job-detail';
import { MyApplications } from './pages/candidate/my-applications/my-applications';
import { Dashboard } from './pages/hr/dashboard/dashboard';
import { HrLayout } from './layouts/hr-layout/hr-layout';
import { ManageApplications } from './pages/hr/manage-applications/manage-applications';
import { ApplicationsManagementComponent } from './pages/hr/applications-management/applications-management.component';
import { EmployeeManagement } from './pages/admin/employee-management/employee-management';
import { ForbiddenComponent } from './pages/forbidden/forbidden.component';
import { roleGuard } from './guards/role.guard';
import { authGuard } from './guards/auth.guard';
import { roleAccessGuard } from './guards/role-access.guard';
import { CandidateDetail } from './components/admin/candidate-detail/candidate-detail';
import { jobResolver } from './resolvers/job-search.resolver';
import { profileResolver } from './resolvers/profile.resolver';
import { candidateLayoutGuard } from './guards/candidate-layout.guard';
import { CodeInterviewComponent } from './pages/candidate/code-interview/code-interview.component';
import { AssessmentAttemptComponent } from './pages/candidate/assessment-attempt/assessment-attempt.component';
import { CodeChallengesComponent } from './pages/hr/code-challenges/code-challenges.component';
import { AnalyticsDashboardComponent } from './pages/hr/analytics-dashboard/analytics-dashboard.component';

export const routes: Routes = [
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  { path: '403', component: ForbiddenComponent },
  {
    path: 'candidate',
    canActivateChild: [candidateLayoutGuard],
    children: [
      { path: 'home', component: Home },
      {
        path: 'jobs',
        loadComponent: () => import('./components/candidate/job-search/job-search').then(m => m.JobSearchComponent),
        resolve: { jobs: jobResolver }
      },
      { path: 'job-detail/:id', component: JobDetail },
      {
        path: 'company',
        loadComponent: () => import('./pages/candidate/company/company').then(m => m.CompanyComponent)
      },
      {
        path: 'my-applications',
        component: MyApplications,
        canActivate: [authGuard]
      },
      {
        path: 'profile',
        loadComponent: () => import('./components/candidate/candidate-profile/candidate-profile').then(m => m.CandidateProfile),
        resolve: { profile: profileResolver },
        canActivate: [authGuard]
      },
      {
        path: 'saved-jobs',
        loadComponent: () => import('./pages/candidate/saved-jobs/saved-jobs').then(m => m.SavedJobsComponent),
        canActivate: [authGuard]
      },
      {
        path: 'settings',
        loadComponent: () => import('./pages/candidate/candidate-settings/candidate-settings').then(m => m.CandidateSettingsComponent),
        canActivate: [authGuard]
      },
      {
        path: 'assessment/:id',
        component: AssessmentAttemptComponent,
        canActivate: [authGuard]
      },
      {
        path: 'code-interview/:id',
        component: CodeInterviewComponent,
        canActivate: [authGuard]
      },
      { path: '', redirectTo: 'home', pathMatch: 'full' }
    ]
  },
  {
    path: 'hr',
    component: HrLayout,
    children: [
      // ─── NHÓM 1: BÀN LÀM VIỆC ─────────────────────────────────────────
      {
        path: 'dashboard',
        component: Dashboard,
        canActivate: [roleAccessGuard]
      },
      {
        path: 'ai-screening',
        loadComponent: () => import('./pages/hr/ai-screening/ai-screening.component').then(m => m.AiScreeningComponent),
        canActivate: [roleAccessGuard]
      },
      {
        path: 'activities',
        loadComponent: () => import('./pages/hr/recent-activities/recent-activities').then(m => m.RecentActivitiesComponent)
      },

      // ─── NHÓM 2: NGHIỆP VỤ TUYỂN DỤNG ──────────────────────────────────
      {
        path: 'jobs',
        loadComponent: () => import('./pages/hr/job-list/job-list').then(m => m.JobListComponent),
        canActivate: [roleAccessGuard]
      },
      {
        path: 'post-job',
        loadComponent: () => import('./pages/hr/post-job').then(m => m.PostJob),
        canActivate: [roleAccessGuard]
      },
      {
        path: 'post-job/:id',
        loadComponent: () => import('./pages/hr/post-job').then(m => m.PostJob),
        canActivate: [roleAccessGuard]
      },
      // Hỗ trợ cả 2 route /applications và /manage-applications
      {
        path: 'applications',
        component: ApplicationsManagementComponent,
        canActivate: [roleAccessGuard]
      },
      {
        path: 'applications/:jobId',
        component: ApplicationsManagementComponent,
        canActivate: [roleAccessGuard]
      },
      {
        path: 'applications-legacy',
        component: ManageApplications,
        canActivate: [roleAccessGuard]
      },
      {
        path: 'manage-applications',
        redirectTo: 'applications',
        pathMatch: 'full'
      },
      {
        path: 'manage-applications/:jobId',
        redirectTo: 'applications/:jobId',
        pathMatch: 'full'
      },
      {
        path: 'candidate-detail',
        component: CandidateDetail
      },
      // Hỗ trợ cả 2 route /interviews và /my-interviews
      {
        path: 'interviews',
        loadComponent: () => import('./components/my-interviews/my-interviews').then(m => m.MyInterviews),
        canActivate: [roleAccessGuard]
      },
      {
        path: 'my-interviews',
        redirectTo: 'interviews',
        pathMatch: 'full'
      },
      // Tiếp nhận & Onboarding mới
      {
        path: 'onboarding',
        loadComponent: () => import('./pages/hr/onboarding/onboarding-management.component').then(m => m.OnboardingManagementComponent),
        canActivate: [roleAccessGuard]
      },
      // Kho nhân tài (Talent Pool) - đổi từ /candidates
      {
        path: 'talent-pool',
        loadComponent: () => import('./pages/admin/candidate-management/candidate-management').then(m => m.CandidateManagementComponent),
        canActivate: [roleAccessGuard]
      },
      {
        path: 'candidates',
        redirectTo: 'talent-pool',
        pathMatch: 'full'
      },

      // ─── NHÓM 3: TỔ CHỨC & ĐÁNH GIÁ ────────────────────────────────────
      {
        path: 'employees',
        component: EmployeeManagement,
        canActivate: [roleAccessGuard]
      },
      {
        path: 'skills',
        loadComponent: () => import('./pages/admin/skill-management/skill-management').then(m => m.SkillManagementComponent),
        canActivate: [roleAccessGuard]
      },
      {
        path: 'question-bank',
        loadComponent: () => import('./pages/hr/question-bank/question-bank').then(m => m.QuestionBankComponent),
        canActivate: [roleAccessGuard]
      },
      {
        path: 'reports',
        loadComponent: () => import('./components/admin/reports/reports').then(m => m.Reports),
        canActivate: [roleAccessGuard],
        data: { ssr: false }
      },

      // ─── NHÓM 4: HỆ THỐNG ──────────────────────────────────────────────
      {
        path: 'chatbot-faq',
        loadComponent: () => import('./pages/admin/chatbot-admin/chatbot-admin').then(m => m.ChatbotAdminComponent),
        canActivate: [roleAccessGuard]
      },
      {
        path: 'chatbot-admin',
        redirectTo: 'chatbot-faq',
        pathMatch: 'full'
      },
      {
        path: 'settings',
        loadComponent: () => import('./pages/hr/settings/settings').then(m => m.SettingsComponent),
        canActivate: [roleAccessGuard]
      },
      {
        path: 'code-challenges',
        component: CodeChallengesComponent,
        canActivate: [roleGuard],
        data: { roles: ['ADMIN', 'HR', 'MANAGER'] }
      },
      {
        path: 'analytics',
        component: AnalyticsDashboardComponent,
        canActivate: [roleGuard],
        data: { roles: ['ADMIN', 'HR', 'MANAGER'] }
      },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },
  {
    path: 'apply/:jobId',
    loadComponent: () => import('./pages/public/qr-apply/qr-apply').then(m => m.QrApplyComponent)
  },
  { path: '', redirectTo: 'candidate/home', pathMatch: 'full' },
  { path: '**', redirectTo: 'candidate/home' }
];
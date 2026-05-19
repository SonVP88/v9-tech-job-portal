import { Routes } from '@angular/router';
import { Login } from './pages/login/login';
import { Register } from './pages/register/register';
import { Home } from './pages/candidate/home/home';
import { JobDetail } from './pages/candidate/job-detail/job-detail';
import { MyApplications } from './pages/candidate/my-applications/my-applications';
import { Dashboard } from './pages/hr/dashboard/dashboard';
import { HrLayout } from './layouts/hr-layout/hr-layout';
import { ManageApplications } from './pages/hr/manage-applications/manage-applications';
import { EmployeeManagement } from './pages/admin/employee-management/employee-management';
import { ForbiddenComponent } from './pages/forbidden/forbidden.component';
import { roleGuard } from './guards/role.guard';
import { authGuard } from './guards/auth.guard';
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
      // Public - không cần đăng nhập
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
      // Private - yêu cầu đăng nhập
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
      { path: 'dashboard', component: Dashboard },
      {
        path: 'activities',
        loadComponent: () => import('./pages/hr/recent-activities/recent-activities').then(m => m.RecentActivitiesComponent)
      },
      {
        path: 'jobs',
        loadComponent: () => import('./pages/hr/job-list/job-list').then(m => m.JobListComponent)
      },
      {
        path: 'post-job',
        loadComponent: () => import('./pages/hr/post-job').then(m => m.PostJob)
      },
      {
        path: 'settings',
        loadComponent: () => import('./pages/hr/settings/settings').then(m => m.SettingsComponent)
      },
      {
        path: 'post-job/:id',
        loadComponent: () => import('./pages/hr/post-job').then(m => m.PostJob)
      },
      { path: 'manage-applications/:jobId', component: ManageApplications },
      { path: 'manage-applications', component: ManageApplications },
      { path: 'candidate-detail', component: CandidateDetail },
      {
        path: 'reports',
        loadComponent: () => import('./components/admin/reports/reports').then(m => m.Reports),
        data: { ssr: false }
      },
      {
        path: 'my-interviews',
        loadComponent: () => import('./components/my-interviews/my-interviews').then(m => m.MyInterviews),
        canActivate: [roleGuard],
        data: { roles: ['INTERVIEWER', 'HR', 'ADMIN'] }
      },
      {
        path: 'employees',
        component: EmployeeManagement,
        canActivate: [roleGuard],
        data: { roles: ['ADMIN', 'HR'] }
      },
      {
        path: 'skills',
        loadComponent: () => import('./pages/admin/skill-management/skill-management').then(m => m.SkillManagementComponent),
        canActivate: [roleGuard],
        data: { roles: ['ADMIN', 'HR', 'INTERVIEW'] }
      },
      {
        path: 'question-bank',
        loadComponent: () => import('./pages/hr/question-bank/question-bank').then(m => m.QuestionBankComponent),
        canActivate: [roleGuard],
        data: { roles: ['ADMIN', 'HR', 'INTERVIEWER'] }
      },
      {
        path: 'chatbot-admin',
        loadComponent: () => import('./pages/admin/chatbot-admin/chatbot-admin').then(m => m.ChatbotAdminComponent),
        canActivate: [roleGuard],
        data: { roles: ['ADMIN', 'HR'] }
      },
      {
        path: 'candidates',
        loadComponent: () => import('./pages/admin/candidate-management/candidate-management').then(m => m.CandidateManagementComponent),
        canActivate: [roleGuard],
        data: { roles: ['ADMIN', 'HR'] }
      },
      {
        path: 'code-challenges',
        component: CodeChallengesComponent,
        canActivate: [roleGuard],
        data: { roles: ['ADMIN', 'HR'] }
      },
      {
        path: 'analytics',
        component: AnalyticsDashboardComponent,
        canActivate: [roleGuard],
        data: { roles: ['ADMIN', 'HR'] }
      },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },
  // Trang chủ mặc định → vào trang job listing (không cần login)
  { path: '', redirectTo: 'candidate/home', pathMatch: 'full' },
  { path: '**', redirectTo: 'candidate/home' }
];
  
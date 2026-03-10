import { Routes } from '@angular/router';
import { Login } from './pages/login/login';
import { Register } from './pages/register/register';
import { Home } from './pages/candidate/home/home';
import { JobDetail } from './pages/candidate/job-detail/job-detail';
import { MyApplications } from './pages/candidate/my-applications/my-applications';
import { Dashboard } from './pages/hr/dashboard/dashboard';
import { HrLayout } from './layouts/hr-layout/hr-layout';
import { PostJob } from './pages/hr/post-job/post-job';
import { ManageApplications } from './pages/hr/manage-applications/manage-applications';
import { EmployeeManagement } from './pages/admin/employee-management/employee-management';
import { ForbiddenComponent } from './pages/forbidden/forbidden.component';
import { roleGuard } from './guards/role.guard';
import { CandidateDetail } from './components/admin/candidate-detail/candidate-detail';
import { jobResolver } from './resolvers/job-search.resolver';
import { profileResolver } from './resolvers/profile.resolver';

export const routes: Routes = [
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  { path: '403', component: ForbiddenComponent },
  {
    path: 'candidate',
    children: [
      { path: 'home', component: Home },
      {
        path: 'jobs',
        loadComponent: () => import('./components/candidate/job-search/job-search').then(m => m.JobSearchComponent),
        resolve: { jobs: jobResolver }
      },
      { path: 'job-detail/:id', component: JobDetail },
      { path: 'my-applications', component: MyApplications },
      {
        path: 'profile',
        loadComponent: () => import('./components/candidate/candidate-profile/candidate-profile').then(m => m.CandidateProfile),
        resolve: { profile: profileResolver }
      },
      {
        path: 'company',
        loadComponent: () => import('./pages/candidate/company/company').then(m => m.CompanyComponent)
      },
      {
        path: 'saved-jobs',
        loadComponent: () => import('./pages/candidate/saved-jobs/saved-jobs').then(m => m.SavedJobsComponent)
      },
      {
        path: 'settings',
        loadComponent: () => import('./pages/candidate/candidate-settings/candidate-settings').then(m => m.CandidateSettingsComponent)
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
      { path: 'post-job', component: PostJob },
      {
        path: 'settings',
        loadComponent: () => import('./pages/hr/settings/settings').then(m => m.SettingsComponent)
      },
      { path: 'post-job/:id', component: PostJob },
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
        data: { roles: ['ADMIN'] }
      },
      {
        path: 'skills',
        loadComponent: () => import('./pages/admin/skill-management/skill-management').then(m => m.SkillManagementComponent),
        canActivate: [roleGuard],
        data: { roles: ['ADMIN'] }
      },
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
    ]
  },
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: '**', redirectTo: 'login' }
];

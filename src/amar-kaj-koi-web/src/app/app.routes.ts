import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './core/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },

  {
    path: 'login',
    loadComponent: () => import('./pages/login/login.component').then((m) => m.LoginComponent),
  },

  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/layout/layout.component').then((m) => m.LayoutComponent),
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./pages/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },

      {
        path: 'tasks',
        loadComponent: () =>
          import('./pages/tasks-list/tasks-list.component').then((m) => m.TasksListComponent),
      },

      {
        path: 'tasks/new-target',
        canActivate: [roleGuard(['TopManagement'])],
        loadComponent: () =>
          import('./pages/new-target/new-target.component').then((m) => m.NewTargetComponent),
      },

      {
        path: 'tasks/new-commitment',
        canActivate: [roleGuard(['Employee'])],
        loadComponent: () =>
          import('./pages/new-commitment/new-commitment.component').then(
            (m) => m.NewCommitmentComponent,
          ),
      },

      {
        path: 'tasks/:id',
        loadComponent: () =>
          import('./pages/task-detail/task-detail.component').then((m) => m.TaskDetailComponent),
      },

      {
        path: 'voice-review',
        canActivate: [roleGuard(['VoiceReviewer'])],
        loadComponent: () =>
          import('./pages/voice-review/voice-review.component').then((m) => m.VoiceReviewComponent),
      },

      {
        path: 'approvals',
        canActivate: [roleGuard(['TopManagement'])],
        loadComponent: () =>
          import('./pages/approvals/approvals.component').then((m) => m.ApprovalsComponent),
      },

      {
        path: 'extend-requests',
        canActivate: [roleGuard(['TopManagement'])],
        loadComponent: () =>
          import('./pages/extend-requests/extend-requests.component').then(
            (m) => m.ExtendRequestsComponent,
          ),
      },

      {
        path: 'notifications',
        loadComponent: () =>
          import('./pages/notifications/notifications.component').then(
            (m) => m.NotificationsComponent,
          ),
      },

      {
        path: 'performance',
        canActivate: [roleGuard(['Employee'])],
        loadComponent: () =>
          import('./pages/performance/performance.component').then((m) => m.PerformanceComponent),
      },

      {
        path: 'performance-all',
        canActivate: [roleGuard(['TopManagement'])],
        loadComponent: () =>
          import('./pages/performance-all/performance-all.component').then(
            (m) => m.PerformanceAllComponent,
          ),
      },

      {
        path: 'my-commitments',
        canActivate: [roleGuard(['Employee'])],
        loadComponent: () =>
          import('./pages/my-commitments/my-commitments.component').then(
            (m) => m.MyCommitmentsComponent,
          ),
      },

      {
        path: 'final-tasks',
        canActivate: [roleGuard(['TopManagement'])],
        loadComponent: () =>
          import('./pages/final-tasks/final-tasks.component').then((m) => m.FinalTasksComponent),
      },

      {
        path: 'event-tasks',
        loadComponent: () =>
          import('./pages/event-tasks/event-tasks.component').then((m) => m.EventTasksComponent),
      },

      {
        path: 'admin/users',
        canActivate: [roleGuard(['TopManagement', 'SystemAdmin'])],
        loadComponent: () => import('./pages/users/users.component').then((m) => m.UsersComponent),
      },
    ],
  },

  { path: '**', redirectTo: 'dashboard' },
];

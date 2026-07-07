import { Routes } from '@angular/router';
import { TaskListComponent } from './features/task-list/task-list.component';

export const routes: Routes = [
  { path: '', redirectTo: '/tasks', pathMatch: 'full' },
  { path: 'tasks', component: TaskListComponent },
  { path: 'tasks/new', loadComponent: () => import('./features/task-form/task-form.component').then(m => m.TaskFormComponent) },
  { path: 'tasks/edit/:id', loadComponent: () => import('./features/task-form/task-form.component').then(m => m.TaskFormComponent) },
  { path: 'tasks/view/:id', loadComponent: () => import('./features/task-detail/task-detail.component').then(m => m.TaskDetailComponent) },
  { path: 'plan', loadComponent: () => import('./features/execution-plan/execution-plan.component').then(m => m.ExecutionPlanComponent) },
  { path: '**', redirectTo: '/tasks' }
];

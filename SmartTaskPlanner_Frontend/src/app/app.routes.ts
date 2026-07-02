import { Routes } from '@angular/router';
import { TaskListComponent } from './features/task-list/task-list.component';
import { TaskFormComponent } from './features/task-form/task-form.component';
import { TaskDetailComponent } from './features/task-detail/task-detail.component';
import { ExecutionPlanComponent } from './features/execution-plan/execution-plan.component';

export const routes: Routes = [
  { path: '', redirectTo: '/tasks', pathMatch: 'full' },
  { path: 'tasks', component: TaskListComponent },
  { path: 'tasks/new', component: TaskFormComponent },
  { path: 'tasks/edit/:id', component: TaskFormComponent },
  { path: 'tasks/view/:id', component: TaskDetailComponent },
  { path: 'plan', component: ExecutionPlanComponent },
  { path: '**', redirectTo: '/tasks' }
];

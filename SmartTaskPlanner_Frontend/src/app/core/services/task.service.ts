import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { tap } from 'rxjs/operators';
import { ApiService } from './api.service';
import { TaskItem, CreateTaskDto, UpdateTaskDto, PagedResponse } from '../models/task.model';

@Injectable({
  providedIn: 'root'
})
export class TaskService {
  constructor(private api: ApiService) {}
  private paginatedTasksSubject = new BehaviorSubject<TaskItem[]>([]);
  public paginatedTasks$ = this.paginatedTasksSubject.asObservable();
  
  public nextCursor: string | null = null;
  public hasNextPage = false;
  public isPageLoading = false;
  public isInitialLoading = true;  // true until the very first page response arrives

  getTaskLookup(): Observable<{id: string, title: string, category: string, status: number}[]> {
    return this.api.get<{id: string, title: string, category: string, status: number}[]>('/tasks/lookup');
  }

  public errorSubject = new BehaviorSubject<string | null>(null);
  public error$ = this.errorSubject.asObservable();

  loadInitialPage(pageSize = 5, search: string = ''): void {
    this.isPageLoading = true;
    let url = `/tasks?pageSize=${pageSize}`;
    if (search) url += `&search=${encodeURIComponent(search)}`;
    this.api.get<PagedResponse<TaskItem>>(url).subscribe({
      next: (res) => {
        this.paginatedTasksSubject.next(res.items);
        this.nextCursor = res.nextCursor;
        this.hasNextPage = res.hasNext;
        this.isPageLoading = false;
        this.isInitialLoading = false; // skeleton dismissed after first load
      },
      error: (err) => {
        this.setError(err.message);
        this.isPageLoading = false;
        this.isInitialLoading = false;
      }
    });
  }

  loadNextPage(pageSize = 5, search: string = ''): void {
    if (!this.hasNextPage || !this.nextCursor || this.isPageLoading) return;
    this.isPageLoading = true;
    let url = `/tasks?pageSize=${pageSize}&cursor=${encodeURIComponent(this.nextCursor)}`;
    if (search) url += `&search=${encodeURIComponent(search)}`;
    this.api.get<PagedResponse<TaskItem>>(url).subscribe({
      next: (res) => {
        const currentTasks = this.paginatedTasksSubject.getValue();
        this.paginatedTasksSubject.next([...currentTasks, ...res.items]);
        this.nextCursor = res.nextCursor;
        this.hasNextPage = res.hasNext;
        this.isPageLoading = false;
      },
      error: (err) => {
        this.setError(err.message);
        this.isPageLoading = false;
      }
    });
  }

  getTask(id: string, forceRefresh = false): Observable<TaskItem> {
    if (!forceRefresh) {
      const cachedTask = this.paginatedTasksSubject.getValue().find(t => t.id === id);
      if (cachedTask) {
        return of(cachedTask); // Return cached task immediately
      }
    }
    return this.api.get<TaskItem>(`/tasks/${id}`);
  }

  createTask(dto: CreateTaskDto, force = false): Observable<TaskItem> {
    return this.api.post<TaskItem>(`/tasks?force=${force}`, dto);
  }

  updateTask(id: string, dto: UpdateTaskDto, force = false): Observable<any> {
    return this.api.put<any>(`/tasks/${id}?force=${force}`, dto);
  }

  deleteTask(id: string): Observable<any> {
    return this.api.delete<any>(`/tasks/${id}`);
  }

  getExecutionPlan(): Observable<TaskItem[]> {
    return this.api.get<TaskItem[]>('/tasks/plan');
  }

  setError(message: string | null) {
    this.errorSubject.next(message);
    if (message) {
      setTimeout(() => this.errorSubject.next(null), 5000); // Auto-clear after 5s
    }
  }
}

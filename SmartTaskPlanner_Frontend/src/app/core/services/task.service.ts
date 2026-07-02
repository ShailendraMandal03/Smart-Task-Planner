import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { tap } from 'rxjs/operators';
import { ApiService } from './api.service';
import { TaskItem, CreateTaskDto, UpdateTaskDto, PagedResponse } from '../models/task.model';

@Injectable({
  providedIn: 'root'
})
export class TaskService {
  private api = inject(ApiService);
  private tasksSubject = new BehaviorSubject<TaskItem[]>([]);
  public tasks$ = this.tasksSubject.asObservable();

  private paginatedTasksSubject = new BehaviorSubject<TaskItem[]>([]);
  public paginatedTasks$ = this.paginatedTasksSubject.asObservable();
  
  public nextCursor: string | null = null;
  public hasNextPage = false;
  public isPageLoading = false;
  
  private hasLoaded = false;

  public errorSubject = new BehaviorSubject<string | null>(null);
  public error$ = this.errorSubject.asObservable();

  constructor() {}

  loadAllTasks(force = false): void {
    if (!force && this.hasLoaded) {
      return; // Return early if tasks are already cached
    }
    this.api.get<TaskItem[]>('/tasks?all=true').subscribe({
      next: (tasks) => {
        this.tasksSubject.next(tasks);
        this.hasLoaded = true;
      },
      error: (err) => this.setError(err.message)
    });
  }

  loadInitialPage(pageSize = 5): void {
    this.isPageLoading = true;
    this.api.get<PagedResponse<TaskItem>>(`/tasks?pageSize=${pageSize}`).subscribe({
      next: (res) => {
        this.paginatedTasksSubject.next(res.items);
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

  loadNextPage(pageSize = 5): void {
    if (!this.hasNextPage || !this.nextCursor || this.isPageLoading) return;
    this.isPageLoading = true;
    const url = `/tasks?pageSize=${pageSize}&cursor=${encodeURIComponent(this.nextCursor)}`;
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
      const cachedTask = this.tasksSubject.getValue().find(t => t.id === id);
      if (cachedTask) {
        return of(cachedTask); // Return cached task immediately
      }
    }
    return this.api.get<TaskItem>(`/tasks/${id}`);
  }

  createTask(dto: CreateTaskDto): Observable<TaskItem> {
    return this.api.post<TaskItem>('/tasks', dto).pipe(
      tap(() => {
        this.loadAllTasks(true);
        this.loadInitialPage();
      })
    );
  }

  updateTask(id: string, dto: UpdateTaskDto): Observable<any> {
    return this.api.put<any>(`/tasks/${id}`, dto).pipe(
      tap(() => {
        this.loadAllTasks(true);
        this.loadInitialPage();
      })
    );
  }

  deleteTask(id: string): Observable<any> {
    return this.api.delete<any>(`/tasks/${id}`).pipe(
      tap(() => {
        this.loadAllTasks(true);
        this.loadInitialPage();
      })
    );
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

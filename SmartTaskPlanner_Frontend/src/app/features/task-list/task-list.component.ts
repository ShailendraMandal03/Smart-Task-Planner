import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TaskService } from '../../core/services/task.service';
import { TaskItem, Priority, TaskStatus, TaskType } from '../../core/models/task.model';
import { map, switchMap, catchError, debounceTime, distinctUntilChanged, withLatestFrom, shareReplay } from 'rxjs/operators';
import { BehaviorSubject, of, Observable } from 'rxjs';

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './task-list.component.html',
  styleUrls: ['./task-list.component.css']
})
export class TaskListComponent implements OnInit {
  private taskService = inject(TaskService);
  
  public Priority = Priority;
  public TaskStatus = TaskStatus;
  public TaskType = TaskType;
  
  public tasks$ = this.taskService.tasks$;
  
  private searchTermSubject = new BehaviorSubject<string>('');
  
  get searchTerm(): string {
    return this.searchTermSubject.value;
  }
  
  set searchTerm(value: string) {
    this.searchTermSubject.next(value);
  }

  /**
   * Matches ONLY a COMPLETE alphanumeric task ID.
   * Requires: prefix letter (D/T/B/G), dash, then min 3 digits.
   * e.g.  D-101, T-201, B-301, G-401 — YES
   *        D-1, D-10 — NO (incomplete, won't fire API)
   */
  private isCompleteTaskId(str: string): boolean {
    const idRegex = /^[DTBG]-\d{3,}$/i;
    return idRegex.test(str);
  }

  /**
   * Use withLatestFrom(tasks$) instead of combineLatest([tasks$, searchTerm$]).
   *
   * WHY: combineLatest fires whenever EITHER source emits. So when tasks$ updates
   * (e.g. after create/delete), the pipeline re-runs with the current search term —
   * causing unwanted duplicate API calls.
   *
   * withLatestFrom only fires when the SEARCH TERM (left side) emits.
   * It just reads a snapshot of tasks$ at that moment without subscribing to it.
   * This means:
   *   - debounceTime(300) correctly suppresses rapid keystrokes
   *   - tasks$ updates do NOT re-trigger the search pipeline
   *   - Only ONE API call fires per debounced, complete ID search
   */
  public filteredTasks$: Observable<TaskItem[]> = this.searchTermSubject.pipe(
    debounceTime(1000), // Wait 1 second after last keystroke before firing
    distinctUntilChanged(),
    switchMap((term) => {
      const trimmedTerm = term.trim();

      // If search is empty, return the paginated tasks
      if (!trimmedTerm) {
        return this.taskService.paginatedTasks$;
      }

      // Only fire GET /tasks/{id} for a COMPLETE ID (e.g. D-101, T-201)
      if (this.isCompleteTaskId(trimmedTerm)) {
        return this.taskService.getTask(trimmedTerm, true).pipe(
          map(task => [task]),
          catchError(() => of([]))
        );
      }

      // Otherwise filter locally from full cache
      return this.taskService.tasks$.pipe(
        map(tasks => tasks.filter(t =>
          t.title.toLowerCase().includes(trimmedTerm.toLowerCase()) ||
          (t.category && t.category.toLowerCase().includes(trimmedTerm.toLowerCase())) ||
          t.id.toLowerCase() === trimmedTerm.toLowerCase()
        ))
      );
    }),
    shareReplay(1) // Share ONE execution among all template subscribers
  );

  get hasNextPage(): boolean {
    return this.taskService.hasNextPage;
  }

  get isPageLoading(): boolean {
    return this.taskService.isPageLoading;
  }

  ngOnInit(): void {
    this.taskService.loadAllTasks();
    this.taskService.loadInitialPage();
  }

  loadMore(): void {
    this.taskService.loadNextPage();
  }

  deleteTask(id: string): void {
    if (confirm('Are you sure you want to delete this task?')) {
      this.taskService.deleteTask(id).subscribe();
    }
  }
}

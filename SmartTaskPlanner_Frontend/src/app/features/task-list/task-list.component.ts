import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TaskService } from '../../core/services/task.service';
import { TaskItem, Priority, TaskStatus, TaskType } from '../../core/models/task.model';
import { ConfirmModalService } from '../../core/confirm-modal/confirm-modal.service';
import { ConfirmModalComponent } from '../../core/confirm-modal/confirm-modal.component';
import { map, switchMap, catchError, debounceTime, distinctUntilChanged, shareReplay } from 'rxjs/operators';
import { BehaviorSubject, of, Observable } from 'rxjs';

@Component({
  selector: 'app-task-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, ConfirmModalComponent],
  templateUrl: './task-list.component.html',
  styleUrls: ['./task-list.component.css']
})
export class TaskListComponent implements OnInit {
  private taskService = inject(TaskService);
  private confirmModal = inject(ConfirmModalService);
  
  public Priority = Priority;
  public TaskStatus = TaskStatus;
  public TaskType = TaskType;
  
  private searchTermSubject = new BehaviorSubject<string>('');
  
  get searchTerm(): string {
    return this.searchTermSubject.value;
  }
  
  set searchTerm(value: string) {
    this.searchTermSubject.next(value);
  }

  public filteredTasks$: Observable<TaskItem[]> = this.taskService.paginatedTasks$;

  get hasNextPage(): boolean {
    return this.taskService.hasNextPage;
  }

  get isPageLoading(): boolean {
    return this.taskService.isPageLoading;
  }

  get isInitialLoading(): boolean {
    return this.taskService.isInitialLoading;
  }

  ngOnInit(): void {
    this.searchTermSubject.pipe(
      debounceTime(500),
      distinctUntilChanged()
    ).subscribe(term => {
      this.taskService.loadInitialPage(5, term.trim());
    });
  }

  loadMore(): void {
    this.taskService.loadNextPage(5, this.searchTerm.trim());
  }


  deleteTask(id: string): void {
    this.confirmModal.open('Are you sure you want to delete this task? This action cannot be undone.')
      .subscribe(confirmed => {
        if (!confirmed) return;
        this.taskService.deleteTask(id).subscribe({
          next: () => {
            this.taskService.loadInitialPage(5, this.searchTerm.trim());
          },
          error: (err) => this.taskService.setError(err.message)
        });
      });
  }
}

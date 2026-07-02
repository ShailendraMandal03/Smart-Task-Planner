import { Component, OnInit, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { switchMap, tap } from 'rxjs/operators';
import { combineLatest } from 'rxjs';
import { TaskService } from '../../core/services/task.service';
import { TaskItem, Priority, TaskStatus, TaskType } from '../../core/models/task.model';

@Component({
  selector: 'app-task-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './task-detail.component.html',
  styleUrls: ['./task-detail.component.css']
})
export class TaskDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private taskService = inject(TaskService);
  private destroyRef = inject(DestroyRef);

  public task: TaskItem | null = null;
  public dependencies: TaskItem[] = [];
  
  public Priority = Priority;
  public TaskStatus = TaskStatus;
  public TaskType = TaskType;

  ngOnInit(): void {
    // 1. Ensure tasks cache is loaded in background without forcing reload
    this.taskService.loadAllTasks();

    // 2. Reactively handle route parameter changes
    this.route.paramMap.pipe(
      switchMap(params => {
        const id = params.get('id');
        // Fetch the specific task. Will hit cache if available, else API.
        return combineLatest([
          this.taskService.getTask(id!),
          this.taskService.tasks$
        ]);
      }),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(([task, allTasks]) => {
      this.task = task;
      // Filter the cached tasks to find the dependencies
      this.dependencies = allTasks.filter(t => task.dependencies.includes(t.id));
    });
  }
}

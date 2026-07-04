import { Component, OnInit, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TaskService } from '../../core/services/task.service';
import { Priority, TaskStatus, TaskType, TaskItem } from '../../core/models/task.model';
import { CycleDialogService } from '../../core/cycle-dialog/cycle-dialog.service';
import { CycleDialogComponent } from '../../core/cycle-dialog/cycle-dialog.component';

@Component({
  selector: 'app-task-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CycleDialogComponent],
  templateUrl: './task-form.component.html',
  styleUrls: ['./task-form.component.css']
})
export class TaskFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private taskService = inject(TaskService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private destroyRef = inject(DestroyRef);
  private cycleDialog = inject(CycleDialogService);

  public taskForm!: FormGroup;
  public isEditMode = false;
  public taskId: string | null = null;
  public allTasks: {id: string, title: string, category: string, status: number}[] = [];
  public errorMessage: string | null = null;

  // Enum references for template
  public priorities = [
    { value: Priority.Low, label: 'Low' },
    { value: Priority.Medium, label: 'Medium' },
    { value: Priority.High, label: 'High' }
  ];
  
  public taskTypes = [
    { value: TaskType.General, label: 'General' },
    { value: TaskType.Development, label: 'Development' },
    { value: TaskType.Testing, label: 'Testing' },
    { value: TaskType.Bug, label: 'Bug' }
  ];

  public statuses = [
    { value: TaskStatus.ToDo, label: 'To Do' },
    { value: TaskStatus.InProgress, label: 'In Progress' },
    { value: TaskStatus.Done, label: 'Done' }
  ];

  ngOnInit(): void {
    this.initForm();
    this.taskId = this.route.snapshot.paramMap.get('id');
    
    this.taskService.getTaskLookup()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(tasks => {
        this.allTasks = this.taskId ? tasks.filter(t => t.id !== this.taskId) : tasks;
      });

    if (this.taskId) {
      this.isEditMode = true;
      this.taskService.getTask(this.taskId).subscribe(task => {
        this.taskForm.patchValue({
          title: task.title,
          description: task.description,
          priority: task.priority,
          estimatedEffort: task.estimatedEffort,
          category: task.category,
          type: task.type,
          status: task.status,
          dependencies: task.dependencies
        });
      });
    }
  }

  private initForm(): void {
    this.taskForm = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(200)]],
      description: [''],
      priority: [Priority.Medium, Validators.required],
      estimatedEffort: [1, [Validators.required, Validators.min(1)]],
      category: [''],
      type: [TaskType.General, Validators.required],
      status: [TaskStatus.ToDo],
      dependencies: [[]]
    });
  }

  onSubmit(): void {
    if (this.taskForm.invalid) return;

    this.errorMessage = null;

    const dto = this.taskForm.value;
    dto.priority = Number(dto.priority);
    dto.type = Number(dto.type);
    dto.status = Number(dto.status);

    this.executeSave(dto, false);
  }

  private executeSave(dto: any, force: boolean): void {
    const request$ = this.isEditMode && this.taskId
      ? this.taskService.updateTask(this.taskId, dto, force)
      : this.taskService.createTask(dto, force);

    request$.subscribe({
      next: (responseTask) => {
        if (!this.isEditMode && responseTask) {
          this.taskForm.reset(); // Reset form for the next task
          // Instead of making another API call, we just inject the new task into our local list!
          this.allTasks = [...this.allTasks, {
            id: responseTask.id,
            title: responseTask.title,
            category: responseTask.category || '',
            status: responseTask.status
          }];
        }
      },
      error: (err) => {
        if (err.status === 409 && err.error?.cyclePath) {
          const cyclePath = err.error.cyclePath;
          this.cycleDialog.open(cyclePath, 'confirm').subscribe(confirmed => {
            if (confirmed) {
              this.executeSave(dto, true); 
            }
          });
        } else {
          const msg = err.message || 'An unknown error occurred';
          this.errorMessage = msg;
          this.taskService.setError(msg);
        }
      }
    });
  }
}


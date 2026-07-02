import { Component, OnInit, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TaskService } from '../../core/services/task.service';
import { Priority, TaskStatus, TaskType, TaskItem } from '../../core/models/task.model';

@Component({
  selector: 'app-task-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './task-form.component.html',
  styleUrls: ['./task-form.component.css']
})
export class TaskFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private taskService = inject(TaskService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private destroyRef = inject(DestroyRef);

  public taskForm!: FormGroup;
  public isEditMode = false;
  public taskId: string | null = null;
  public allTasks: TaskItem[] = [];
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
    
    // Load all tasks for the dependencies dropdown.
    // takeUntilDestroyed(this.destroyRef) automatically unsubscribes when the
    // component is destroyed, preventing a memory leak.
    this.taskService.tasks$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(tasks => {
        // Exclude self if editing so a task cannot depend on itself
        this.allTasks = this.taskId ? tasks.filter(t => t.id !== this.taskId) : tasks;
      });
    this.taskService.loadAllTasks();

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
    // Map string values back to numbers if needed from select
    dto.priority = Number(dto.priority);
    dto.type = Number(dto.type);
    dto.status = Number(dto.status);

    const request$ = this.isEditMode && this.taskId
      ? this.taskService.updateTask(this.taskId, dto)
      : this.taskService.createTask(dto);

    request$.subscribe({
      next: () => this.router.navigate(['/tasks']),
      error: (err) => {
        this.errorMessage = err.message;
        this.taskService.setError(err.message);
      }
    });
  }
}


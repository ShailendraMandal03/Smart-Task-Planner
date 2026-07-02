import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TaskService } from '../../core/services/task.service';
import { TaskItem, Priority, TaskType } from '../../core/models/task.model';

@Component({
  selector: 'app-execution-plan',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './execution-plan.component.html',
  styleUrls: ['./execution-plan.component.css']
})
export class ExecutionPlanComponent implements OnInit {
  private taskService = inject(TaskService);

  public executionPlan: TaskItem[] = [];
  public isLoading = true;
  public errorMessage: string | null = null;

  public Priority = Priority;
  public TaskType = TaskType;

  ngOnInit(): void {
    this.loadExecutionPlan();
  }

  loadExecutionPlan(): void {
    this.isLoading = true;
    this.errorMessage = null;

    this.taskService.getExecutionPlan().subscribe({
      next: (plan) => {
        this.executionPlan = plan;
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.message;
        this.taskService.setError(err.message);
        this.isLoading = false;
      }
    });
  }
}

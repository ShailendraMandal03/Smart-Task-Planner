import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TaskService } from '../../core/services/task.service';
import { TaskItem, Priority, TaskType } from '../../core/models/task.model';
import { CycleDialogService } from '../../core/cycle-dialog/cycle-dialog.service';
import { CycleDialogComponent } from '../../core/cycle-dialog/cycle-dialog.component';

@Component({
  selector: 'app-execution-plan',
  standalone: true,
  imports: [CommonModule, RouterLink, CycleDialogComponent],
  templateUrl: './execution-plan.component.html',
  styleUrls: ['./execution-plan.component.css']
})
export class ExecutionPlanComponent implements OnInit {
  constructor(private taskService: TaskService, private cycleDialog: CycleDialogService) {}

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
        if (err.status === 409 && err.error?.cyclePath) {
          const cyclePath = err.error.cyclePath;
          this.cycleDialog.open(cyclePath, 'error');
          // Don't show a top-level error message since the modal handles it
          this.errorMessage = 'Execution plan could not be generated due to circular dependencies.';
        } else {
          const msg = err.message || 'An unknown error occurred';
          this.errorMessage = msg;
          this.taskService.setError(msg);
        }
        this.isLoading = false;
      }
    });
  }
}

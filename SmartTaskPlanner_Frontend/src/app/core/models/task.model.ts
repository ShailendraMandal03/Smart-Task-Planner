export enum Priority {
  Low = 1,
  Medium = 2,
  High = 3
}

export enum TaskType {
  General = 0,
  Development = 1,
  Testing = 2,
  Bug = 3
}

export enum TaskStatus {
  ToDo = 0,
  InProgress = 1,
  Done = 2
}

export interface TaskItem {
  id: string;
  title: string;
  description: string;
  priority: Priority;
  estimatedEffort: number;
  category: string;
  type: TaskType;
  dependencies: string[];
  status: TaskStatus;
  createdAt: Date;
}

export interface CreateTaskDto {
  title: string;
  description: string;
  priority: Priority;
  estimatedEffort: number;
  category: string;
  type: TaskType;
  dependencies: string[];
}

export interface UpdateTaskDto extends CreateTaskDto {
  status: TaskStatus;
}

export interface PagedResponse<T> {
  items: T[];
  nextCursor: string | null;
  hasNext: boolean;
}

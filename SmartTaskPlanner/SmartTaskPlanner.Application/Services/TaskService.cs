using SmartTaskPlanner.Application.DTOs;
using SmartTaskPlanner.Application.Interfaces;
using SmartTaskPlanner.Domain.Entities;
using SmartTaskPlanner.Domain.Exceptions;
using SmartTaskPlanner.Domain.Factories;
using Microsoft.Extensions.Logging;

namespace SmartTaskPlanner.Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IGraphService _graphService;
    private readonly ITaskFactory _taskFactory;
    private readonly ILogger<TaskService> _logger;

    public TaskService(ITaskRepository taskRepository, IGraphService graphService, ITaskFactory taskFactory, ILogger<TaskService> logger)
    {
        _taskRepository = taskRepository;
        _graphService = graphService;
        _taskFactory = taskFactory;
        _logger = logger;
    }

    public async Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync()
    {
        _logger.LogInformation("Retrieving all tasks from repository");
        var tasks = await _taskRepository.GetAllAsync();
        return tasks.Select(MapToDto);
    }

    public async Task<PagedResponseDto<TaskResponseDto>> GetPagedTasksAsync(string? cursor, int pageSize)
    {
        _logger.LogInformation("Retrieving paged tasks with pageSize {PageSize} and cursor {Cursor}", pageSize, cursor);
        
        // Fetch pageSize + 1 to check if there is a next page
        var tasks = (await _taskRepository.GetPagedAsync(cursor, pageSize + 1)).ToList();
        
        var hasNext = tasks.Count > pageSize;
        var itemsToReturn = tasks.Take(pageSize).ToList();

        string? nextCursor = null;
        if (hasNext && itemsToReturn.Any())
        {
            var lastItem = itemsToReturn.Last();
            var cursorValue = $"{lastItem.CreatedAt:O}|{lastItem.Id}";
            var cursorBytes = System.Text.Encoding.UTF8.GetBytes(cursorValue);
            nextCursor = Convert.ToBase64String(cursorBytes);
        }

        return new PagedResponseDto<TaskResponseDto>(
            itemsToReturn.Select(MapToDto),
            nextCursor,
            hasNext);
    }

    public async Task<TaskResponseDto> GetTaskByIdAsync(string id)
    {
        _logger.LogInformation("Retrieving task with ID: {TaskId}", id);
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null)
        {
            _logger.LogWarning("Task with ID {TaskId} not found", id);
            throw new TaskNotFoundException($"Task with ID {id} not found.");
        }

        return MapToDto(task);
    }

    public async Task<TaskResponseDto> CreateTaskAsync(CreateTaskDto dto)
    {
        _logger.LogInformation("Creating new task with Title: {Title}, Type: {Type}", dto.Title, dto.Type);
        var newTask = _taskFactory.Create(
            dto.Title, 
            dto.Description, 
            dto.Priority, 
            dto.EstimatedEffort, 
            dto.Category, 
            dto.Type, 
            dto.Dependencies);

        var allTasks = await _taskRepository.GetAllAsync();
        _logger.LogInformation("Validating graph for new task dependencies");
        _graphService.EnsureNoCyclesOrInvalidDependencies(allTasks, newTask);

        var createdTask = await _taskRepository.AddAsync(newTask);
        _logger.LogInformation("Successfully saved task with ID: {TaskId} to repository", createdTask.Id);
        return MapToDto(createdTask);
    }

    public async Task UpdateTaskAsync(string id, UpdateTaskDto dto)
    {
        _logger.LogInformation("Updating task with ID: {TaskId}", id);
        var existingTask = await _taskRepository.GetByIdAsync(id);
        if (existingTask == null)
        {
            _logger.LogWarning("Task with ID {TaskId} not found for update", id);
            throw new TaskNotFoundException($"Task with ID {id} not found.");
        }

        existingTask.Title = dto.Title;
        existingTask.Description = dto.Description;
        existingTask.Priority = dto.Priority;
        existingTask.EstimatedEffort = dto.EstimatedEffort;
        existingTask.Category = dto.Category;
        existingTask.Type = dto.Type;
        existingTask.Status = dto.Status;
        existingTask.Dependencies = dto.Dependencies ?? new List<string>();

        // Re-apply factory business rules on every update.
        // This ensures rules like "Bug tasks are always High priority" cannot
        // be bypassed by sending a PUT request with a lower priority value.
        _taskFactory.ApplyBusinessRules(existingTask);

        var allTasks = await _taskRepository.GetAllAsync();
        _logger.LogInformation("Validating graph for updated task dependencies");
        _graphService.EnsureNoCyclesOrInvalidDependencies(allTasks, existingTask);

        await _taskRepository.UpdateAsync(existingTask);
        _logger.LogInformation("Successfully updated task with ID: {TaskId} in repository", id);
    }

    public async Task DeleteTaskAsync(string id)
    {
        _logger.LogInformation("Attempting to delete task with ID: {TaskId}", id);
        var existingTask = await _taskRepository.GetByIdAsync(id);
        if (existingTask == null)
        {
            _logger.LogWarning("Task with ID {TaskId} not found for deletion", id);
            throw new TaskNotFoundException($"Task with ID {id} not found.");
        }

        // Check if any other task depends on this one
        var allTasks = await _taskRepository.GetAllAsync();
        if (allTasks.Any(t => t.Dependencies.Contains(id)))
        {
            _logger.LogWarning("Cannot delete task {TaskId} - dependency constraint violated", id);
            throw new DomainException($"Cannot delete task {id} because other tasks depend on it.");
        }

        await _taskRepository.DeleteAsync(id);
        _logger.LogInformation("Successfully deleted task with ID: {TaskId} from repository", id);
    }

    public async Task<IEnumerable<TaskResponseDto>> GenerateExecutionPlanAsync()
    {
        var allTasks = await _taskRepository.GetAllAsync();
        var plan = _graphService.GenerateExecutionPlan(allTasks);
        return plan.Select(MapToDto);
    }

    private static TaskResponseDto MapToDto(TaskItem task)
    {
        return new TaskResponseDto(
            task.Id,
            task.Title,
            task.Description,
            task.Priority,
            task.EstimatedEffort,
            task.Category,
            task.Type,
            task.Dependencies,
            task.Status,
            task.CreatedAt
        );
    }
}

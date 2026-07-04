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

    public async Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Retrieving all tasks from repository");
        var tasks = await _taskRepository.GetAllAsync(ct);
        return tasks.Select(MapToDto);
    }

    public async Task<PagedResponseDto<TaskResponseDto>> GetPagedTasksAsync(string? cursor, int pageSize, string? search = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Retrieving paged tasks with pageSize {PageSize}, cursor {Cursor}, search {Search}", pageSize, cursor, search);
        
        var tasks = (await _taskRepository.GetPagedAsync(cursor, pageSize + 1, search, ct)).ToList();
        
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

    public async Task<TaskResponseDto> GetTaskByIdAsync(string id, CancellationToken ct = default)
    {
        _logger.LogInformation("Retrieving task with ID: {TaskId}", id);
        var task = await _taskRepository.GetByIdAsync(id, ct);
        if (task == null)
        {
            _logger.LogWarning("Task with ID {TaskId} not found", id);
            throw new TaskNotFoundException($"Task with ID {id} not found.");
        }

        return MapToDto(task);
    }

    public async Task<IEnumerable<TaskLookupDto>> GetTaskLookupAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Retrieving all tasks for lookup");
        var tasks = await _taskRepository.GetAllAsync(ct);
        return tasks.Select(t => new TaskLookupDto(t.Id, t.Title, t.Category, t.Status));
    }

    public async Task<TaskResponseDto> CreateTaskAsync(CreateTaskDto dto, bool force = false, CancellationToken ct = default)
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

        var allTasks = await _taskRepository.GetAllAsync(ct);
        _logger.LogInformation("Validating graph for new task dependencies");
        
        try
        {
            _graphService.ValidateGraph(allTasks, newTask);
        }
        catch (CircularDependencyException) when (force)
        {
            _logger.LogWarning("Bypassing circular dependency check for new task due to force flag.");
        }

        var createdTask = await _taskRepository.AddAsync(newTask, ct);
        _logger.LogInformation("Successfully saved task with ID: {TaskId} to repository", createdTask.Id);
        return MapToDto(createdTask);
    }

    public async Task UpdateTaskAsync(string id, UpdateTaskDto dto, bool force = false, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating task with ID: {TaskId}", id);
        var existingTask = await _taskRepository.GetByIdAsync(id, ct);
        if (existingTask == null)
        {
            _logger.LogWarning("Task with ID {TaskId} not found for update", id);
            throw new TaskNotFoundException($"Task with ID {id} not found.");
        }

        // Create a temporary clone for validation to prevent mutating memory if validation fails
        var tempTask = new TaskItem(
            id: existingTask.Id,
            title: dto.Title,
            description: dto.Description,
            priority: dto.Priority,
            estimatedEffort: dto.EstimatedEffort,
            category: dto.Category,
            type: dto.Type,
            dependencies: dto.Dependencies ?? new List<string>(),
            status: dto.Status,
            createdAt: existingTask.CreatedAt);
            
        _taskFactory.ApplyBusinessRules(tempTask);

        // Validating  the temporary clone data
        var allTasks = await _taskRepository.GetAllAsync(ct);
        _logger.LogInformation("Validating graph for updated task dependencies");
        try
        {
            _graphService.ValidateGraph(allTasks, tempTask);
        }
        catch (CircularDependencyException) when (force)
        {
            _logger.LogWarning("Bypassing circular dependency check for updated task {TaskId} due to force flag.", id);
        }

        // If validation passed (or was forced), safely update the real entity in memory
        existingTask.Update(
            dto.Title,
            dto.Description,
            dto.Priority,
            dto.EstimatedEffort,
            dto.Category,
            dto.Type,
            dto.Status,
            dto.Dependencies ?? new List<string>());

        _taskFactory.ApplyBusinessRules(existingTask);

        await _taskRepository.UpdateAsync(existingTask, ct);
        _logger.LogInformation("Successfully updated task with ID: {TaskId} in repository", id);
    }

    public async Task DeleteTaskAsync(string id, CancellationToken ct = default)
    {
        _logger.LogInformation("Attempting to delete task with ID: {TaskId}", id);
        var existingTask = await _taskRepository.GetByIdAsync(id, ct);
        if (existingTask == null)
        {
            _logger.LogWarning("Task with ID {TaskId} not found for deletion", id);
            throw new TaskNotFoundException($"Task with ID {id} not found.");
        }

        var allTasks = await _taskRepository.GetAllAsync(ct);
        if (allTasks.Any(t => t.Dependencies.Contains(id)))
        {
            _logger.LogWarning("Cannot delete task {TaskId} - dependency constraint violated", id);
            throw new DomainException($"Cannot delete task {id} because other tasks depend on it.");
        }

        await _taskRepository.DeleteAsync(id, ct);
        _logger.LogInformation("Successfully deleted task with ID: {TaskId} from repository", id);
    }

    public async Task<IEnumerable<TaskResponseDto>> GenerateExecutionPlanAsync(CancellationToken ct = default)
    {
        var allTasks = await _taskRepository.GetAllAsync(ct);
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

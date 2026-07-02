using SmartTaskPlanner.Application.DTOs;

namespace SmartTaskPlanner.Application.Interfaces;

public interface ITaskService
{
    Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync();
    Task<PagedResponseDto<TaskResponseDto>> GetPagedTasksAsync(string? cursor, int pageSize);
    Task<TaskResponseDto> GetTaskByIdAsync(string id);
    Task<TaskResponseDto> CreateTaskAsync(CreateTaskDto dto);
    Task UpdateTaskAsync(string id, UpdateTaskDto dto);
    Task DeleteTaskAsync(string id);
    Task<IEnumerable<TaskResponseDto>> GenerateExecutionPlanAsync();
}

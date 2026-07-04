using SmartTaskPlanner.Application.DTOs;

namespace SmartTaskPlanner.Application.Interfaces;

public interface ITaskService
{
    Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync(CancellationToken ct = default);
    Task<PagedResponseDto<TaskResponseDto>> GetPagedTasksAsync(string? cursor, int pageSize, string? search = null, CancellationToken ct = default);
    Task<IEnumerable<TaskLookupDto>> GetTaskLookupAsync(CancellationToken ct = default);
    Task<TaskResponseDto> GetTaskByIdAsync(string id, CancellationToken ct = default);
    Task<TaskResponseDto> CreateTaskAsync(CreateTaskDto dto, bool force = false, CancellationToken ct = default);
    Task UpdateTaskAsync(string id, UpdateTaskDto dto, bool force = false, CancellationToken ct = default);
    Task DeleteTaskAsync(string id, CancellationToken ct = default);
    Task<IEnumerable<TaskResponseDto>> GenerateExecutionPlanAsync(CancellationToken ct = default);
}

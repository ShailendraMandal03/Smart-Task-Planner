using SmartTaskPlanner.Domain.Entities;

namespace SmartTaskPlanner.Application.Interfaces;

public interface ITaskRepository
{
    Task<IEnumerable<TaskItem>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<TaskItem>> GetPagedAsync(string? cursor, int pageSize, string? search = null, CancellationToken ct = default);
    Task<TaskItem?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<IEnumerable<TaskItem>> GetManyByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default);
    Task<TaskItem> AddAsync(TaskItem task, CancellationToken ct = default);
    Task UpdateAsync(TaskItem task, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

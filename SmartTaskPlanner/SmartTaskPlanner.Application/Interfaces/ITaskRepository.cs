using SmartTaskPlanner.Domain.Entities;

namespace SmartTaskPlanner.Application.Interfaces;

public interface ITaskRepository
{
    Task<IEnumerable<TaskItem>> GetAllAsync();
    Task<IEnumerable<TaskItem>> GetPagedAsync(string? cursor, int pageSize);
    Task<TaskItem?> GetByIdAsync(string id);
    Task<TaskItem> AddAsync(TaskItem task);
    Task UpdateAsync(TaskItem task);
    Task DeleteAsync(string id);
}

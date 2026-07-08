using SmartTaskPlanner.Domain.Entities;

namespace SmartTaskPlanner.Application.Interfaces;

public interface IGraphService
{
    Task ValidateGraphAsync(TaskItem newTaskOrUpdatedTask, CancellationToken ct = default);

    IEnumerable<TaskItem> GenerateExecutionPlan(IEnumerable<TaskItem> allTasks);
}

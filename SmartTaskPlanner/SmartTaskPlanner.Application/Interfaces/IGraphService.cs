using SmartTaskPlanner.Domain.Entities;

namespace SmartTaskPlanner.Application.Interfaces;

public interface IGraphService
{
    void EnsureNoCyclesOrInvalidDependencies(IEnumerable<TaskItem> allTasks, TaskItem newTaskOrUpdatedTask);
    IEnumerable<TaskItem> GenerateExecutionPlan(IEnumerable<TaskItem> allTasks);
}

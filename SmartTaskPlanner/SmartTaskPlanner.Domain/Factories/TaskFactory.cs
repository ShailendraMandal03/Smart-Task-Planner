using SmartTaskPlanner.Domain.Entities;
using SmartTaskPlanner.Domain.Enums;

namespace SmartTaskPlanner.Domain.Factories;

public interface ITaskFactory
{
    TaskItem Create(string title, string description, Priority priority, int estimatedEffort, string category, TaskType type, List<string> dependencies);

    /// <summary>
    /// Applies type-specific business rules to an existing task in-place.
    /// Called on both Create and Update to ensure rules are never bypassed.
    /// Rules:
    ///  - Bug tasks are always High priority.
    ///  - Testing tasks default to "Quality Assurance" category when none is provided.
    ///  - Development tasks have a minimum estimated effort of 1.
    /// </summary>
    void ApplyBusinessRules(TaskItem task);
}

public class TaskFactory : ITaskFactory
{
    public TaskItem Create(string title, string description, Priority priority, int estimatedEffort, string category, TaskType type, List<string> dependencies)
    {
        var task = new TaskItem(
            id: string.Empty,          
            title: title,
            description: description,
            priority: priority,
            estimatedEffort: estimatedEffort,
            category: category,
            type: type,
            dependencies: dependencies ?? new List<string>(),
            status: Enums.TaskStatus.ToDo,
            createdAt: DateTime.UtcNow);

        ApplyBusinessRules(task);

        return task;
    }

    /// <inheritdoc/>
    public void ApplyBusinessRules(TaskItem task)
    {
        // These internal setters are visible only within the Domain assembly.
        // External layers (Application, Infrastructure, API) cannot call them.
        switch (task.Type)
        {
            case TaskType.Bug:
                task.SetPriority(Priority.High);
                break;

            case TaskType.Testing:
                if (string.IsNullOrWhiteSpace(task.Category))
                {
                    task.SetCategory("Quality Assurance");
                }
                break;

            case TaskType.Development:
                if (task.EstimatedEffort < 1)
                {
                    task.SetEstimatedEffort(1);
                }
                break;
        }
    }
}

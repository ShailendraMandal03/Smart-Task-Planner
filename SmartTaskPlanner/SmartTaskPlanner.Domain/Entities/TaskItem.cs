using SmartTaskPlanner.Domain.Enums;

namespace SmartTaskPlanner.Domain.Entities;

public class TaskItem
{
    public string Id { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Priority Priority { get; private set; }
    public int EstimatedEffort { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public List<string> Dependencies { get; private set; } = new();
    public Enums.TaskStatus Status { get; private set; } = Enums.TaskStatus.ToDo;
    public TaskType Type { get; private set; } = TaskType.General;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;



    public TaskItem(
        string id,
        string title,
        string description,
        Priority priority,
        int estimatedEffort,
        string category,
        TaskType type,
        List<string> dependencies,
        Enums.TaskStatus status,
        DateTime createdAt)
    {
        Id = id;
        Title = title;
        Description = description;
        Priority = priority;
        EstimatedEffort = estimatedEffort;
        Category = category;
        Type = type;
        Dependencies = dependencies ?? new List<string>();
        Status = status;
        CreatedAt = createdAt;
    }

    // ── Mutation methods — the only way to change state after construction ───

    public void AssignId(string id) => Id = id;

    public void Update(
        string title,
        string description,
        Priority priority,
        int estimatedEffort,
        string category,
        TaskType type,
        Enums.TaskStatus status,
        List<string> dependencies)
    {
        Title = title;
        Description = description;
        Priority = priority;
        EstimatedEffort = estimatedEffort;
        Category = category;
        Type = type;
        Status = status;
        Dependencies = dependencies ?? new List<string>();
    }

    internal void SetPriority(Priority priority) => Priority = priority;
    internal void SetCategory(string category) => Category = category;
    internal void SetEstimatedEffort(int effort) => EstimatedEffort = effort;
}

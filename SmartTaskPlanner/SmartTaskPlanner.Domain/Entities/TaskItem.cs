using SmartTaskPlanner.Domain.Enums;

namespace SmartTaskPlanner.Domain.Entities;

/// <summary>
/// Core task aggregate. All properties use <c>private set</c> so that
/// business rules encoded in <see cref="Factories.TaskFactory"/> cannot be
/// bypassed by external code setting properties directly.
///
/// Mutation is only possible through:
///  - The public constructor (used by TaskFactory and seed data)
///  - <see cref="Update"/> — called by TaskService on PUT requests
///  - <see cref="AssignId"/> — called by InMemoryTaskRepository after persisting
/// </summary>
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



    /// <summary>
    /// Full constructor. Used by <see cref="Factories.TaskFactory"/> to create
    /// new tasks, and by <see cref="Infrastructure"/> seed data to build
    /// pre-seeded tasks with known IDs.
    /// </summary>
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

    /// <summary>
    /// Assigns the generated ID. Called once by the repository's
    /// <c>AddAsync</c> after the task has been persisted with an empty ID.
    /// </summary>
    public void AssignId(string id) => Id = id;

    /// <summary>
    /// Applies updated field values from a PUT request.
    /// <see cref="Application.Services.TaskService"/> calls this, followed
    /// immediately by <c>TaskFactory.ApplyBusinessRules</c> to ensure
    /// type-specific rules (e.g. Bug → High priority) cannot be bypassed.
    /// </summary>
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

    /// <summary>
    /// Allows <see cref="Factories.TaskFactory.ApplyBusinessRules"/> to
    /// override individual fields (Priority, Category, EstimatedEffort)
    /// after <see cref="Update"/> has been called.
    /// </summary>
    internal void SetPriority(Priority priority) => Priority = priority;
    internal void SetCategory(string category) => Category = category;
    internal void SetEstimatedEffort(int effort) => EstimatedEffort = effort;
}

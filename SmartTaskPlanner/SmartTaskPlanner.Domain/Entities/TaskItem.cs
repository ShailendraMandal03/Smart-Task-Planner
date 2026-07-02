using SmartTaskPlanner.Domain.Enums;

namespace SmartTaskPlanner.Domain.Entities;

public class TaskItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public int EstimatedEffort { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<string> Dependencies { get; set; } = new();
    public Enums.TaskStatus Status { get; set; } = Enums.TaskStatus.ToDo;
    public TaskType Type { get; set; } = TaskType.General;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

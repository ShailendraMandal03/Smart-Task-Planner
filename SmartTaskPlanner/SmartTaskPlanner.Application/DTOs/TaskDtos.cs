using SmartTaskPlanner.Domain.Enums;
using TaskStatus = SmartTaskPlanner.Domain.Enums.TaskStatus;

namespace SmartTaskPlanner.Application.DTOs;

public record CreateTaskDto(
    string Title,
    string Description,
    Priority Priority,
    int EstimatedEffort,
    string Category,
    TaskType Type,
    List<string> Dependencies);

public record UpdateTaskDto(
    string Title,
    string Description,
    Priority Priority,
    int EstimatedEffort,
    string Category,
    TaskType Type,
    TaskStatus Status,
    List<string> Dependencies);

public record TaskResponseDto(
    string Id,
    string Title,
    string Description,
    Priority Priority,
    int EstimatedEffort,
    string Category,
    TaskType Type,
    List<string> Dependencies,
    TaskStatus Status,
    DateTime CreatedAt);

public record PagedResponseDto<T>(
    IEnumerable<T> Items,
    string? NextCursor,
    bool HasNext);

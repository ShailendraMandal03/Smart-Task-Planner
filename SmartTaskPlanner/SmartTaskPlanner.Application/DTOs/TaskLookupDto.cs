using SmartTaskPlanner.Domain.Enums;

namespace SmartTaskPlanner.Application.DTOs;

public record TaskLookupDto(string Id, string Title, string? Category, SmartTaskPlanner.Domain.Enums.TaskStatus Status);

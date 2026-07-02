using System.Collections.Concurrent;
using SmartTaskPlanner.Application.Interfaces;
using SmartTaskPlanner.Domain.Entities;
using SmartTaskPlanner.Domain.Enums;

namespace SmartTaskPlanner.Infrastructure.Repositories;

public class InMemoryTaskRepository : ITaskRepository
{
    private readonly ConcurrentDictionary<string, TaskItem> _tasks = new();

    public InMemoryTaskRepository()
    {
        //
        // This seed represents a realistic software project lifecycle with:
        //  - All TaskTypes: General, Development, Testing, Bug
        //  - All Priority levels: Low, Medium, High
        //  - Multi-level dependency chains (up to 3 levels deep)
        //  - Parallel task branches that converge before deployment
        //  - Tasks that demonstrate the priority-aware topological sort
        //
        // Dependency graph (→ means "must complete before"):
        //   G-401 → D-101, D-102, T-201
        //   D-101 → D-103, T-202
        //   D-102 → D-104, T-202
        //   D-103 → T-401
        //   D-104 → T-203
        //   T-202 → G-402
        //   T-203 → G-402
        //   B-301 → (no dependents — standalone critical bug fix)
        //   G-403 → (no dependencies — parallel documentation task)
        // -----------------------------------------------------------------------


        var g401 = new TaskItem
        {
            Id = "G-401",
            Title = "Project Bootstrapping & CI Setup",
            Description = "Initialize the Git repository, set up the solution structure, configure CI pipeline (GitHub Actions), and establish code style guidelines (EditorConfig, linting).",
            Priority = Priority.High,
            EstimatedEffort = 3,
            Category = "DevOps",
            Type = TaskType.General,
            Dependencies = new List<string>(),
            Status = Domain.Enums.TaskStatus.Done,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var b301 = new TaskItem
        {
            Id = "B-301",
            Title = "Fix: API returns 500 on empty dependency list",
            Description = "When creating a task with an empty dependency array ([]), the API throws a NullReferenceException instead of accepting it as valid input. Reproduces in dev environment.",
            Priority = Priority.High,     // Overridden to High by TaskFactory for Bug type
            EstimatedEffort = 1,
            Category = "Backend",
            Type = TaskType.Bug,
            Dependencies = new List<string>(),
            Status = Domain.Enums.TaskStatus.InProgress,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        var g403 = new TaskItem
        {
            Id = "G-403",
            Title = "Write API Documentation (Swagger Annotations)",
            Description = "Add XML documentation comments to all controller actions and DTOs. Configure Swagger to display descriptions, example payloads, and response schemas.",
            Priority = Priority.Low,
            EstimatedEffort = 2,
            Category = "Documentation",
            Type = TaskType.General,
            Dependencies = new List<string>(),
            Status = Domain.Enums.TaskStatus.ToDo,
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };

        // ── Level 1: Core Backend & Frontend (depends on G-401) ────────────────

        var d101 = new TaskItem
        {
            Id = "D-101",
            Title = "Implement Authentication Service (JWT)",
            Description = "Build the login and token-refresh endpoints. Integrate ASP.NET Identity. Generate signed JWT tokens with appropriate claims for role-based access.",
            Priority = Priority.High,
            EstimatedEffort = 8,
            Category = "Backend",
            Type = TaskType.Development,
            Dependencies = new List<string> { "G-401" },
            Status = Domain.Enums.TaskStatus.InProgress,
            CreatedAt = DateTime.UtcNow.AddDays(-9)
        };

        var d102 = new TaskItem
        {
            Id = "D-102",
            Title = "Build Core Task Management API",
            Description = "Implement all CRUD endpoints for tasks (/api/tasks). Apply FluentValidation, dependency graph validation, and wire up the execution plan endpoint (/api/tasks/plan).",
            Priority = Priority.High,
            EstimatedEffort = 6,
            Category = "Backend",
            Type = TaskType.Development,
            Dependencies = new List<string> { "G-401" },
            Status = Domain.Enums.TaskStatus.InProgress,
            CreatedAt = DateTime.UtcNow.AddDays(-9)
        };

        var t201 = new TaskItem
        {
            Id = "T-201",
            Title = "Integration Test: Bootstrapping & Configuration",
            Description = "Verify that the application starts correctly, Serilog is configured, Swagger UI is accessible, and CORS headers are present in responses.",
            Priority = Priority.Medium,
            EstimatedEffort = 2,
            Category = "Quality Assurance",
            Type = TaskType.Testing,
            Dependencies = new List<string> { "G-401" },
            Status = Domain.Enums.TaskStatus.ToDo,
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };

  

        var d103 = new TaskItem
        {
            Id = "D-103",
            Title = "Build Login & Registration UI (Angular)",
            Description = "Create Angular components for login, registration, and forgot-password flows. Implement JWT interceptor to attach the Bearer token to all subsequent API calls.",
            Priority = Priority.High,
            EstimatedEffort = 5,
            Category = "Frontend",
            Type = TaskType.Development,
            Dependencies = new List<string> { "D-101" },
            Status = Domain.Enums.TaskStatus.ToDo,
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        };

        var d104 = new TaskItem
        {
            Id = "D-104",
            Title = "Build Task List & Execution Plan UI (Angular)",
            Description = "Implement the main task dashboard: task list with filtering, task creation/edit form, task detail view, and the execution plan visualisation panel.",
            Priority = Priority.High,
            EstimatedEffort = 7,
            Category = "Frontend",
            Type = TaskType.Development,
            Dependencies = new List<string> { "D-102" },
            Status = Domain.Enums.TaskStatus.ToDo,
            CreatedAt = DateTime.UtcNow.AddDays(-7)
        };

        var t202 = new TaskItem
        {
            Id = "T-202",
            Title = "Unit Tests: TaskService & GraphService",
            Description = "Write unit tests for GraphService (cycle detection, topological sort, priority ordering) and TaskService (CRUD orchestration, dependency validation). Use xUnit + Moq.",
            Priority = Priority.High,
            EstimatedEffort = 4,
            Category = "Quality Assurance",
            Type = TaskType.Testing,
            Dependencies = new List<string> { "D-101", "D-102" },
            Status = Domain.Enums.TaskStatus.ToDo,
            CreatedAt = DateTime.UtcNow.AddDays(-6)
        };

    

        var t203 = new TaskItem
        {
            Id = "T-203",
            Title = "End-to-End Tests: Full User Journey",
            Description = "Write E2E tests (Cypress or Playwright) covering: user login → view tasks → create task with dependencies → view execution plan → mark task complete → delete task.",
            Priority = Priority.Medium,
            EstimatedEffort = 6,
            Category = "Quality Assurance",
            Type = TaskType.Testing,
            Dependencies = new List<string> { "D-103", "D-104" },
            Status = Domain.Enums.TaskStatus.ToDo,
            CreatedAt = DateTime.UtcNow.AddDays(-4)
        };


        var g402 = new TaskItem
        {
            Id = "G-402",
            Title = "Deploy to Staging Environment",
            Description = "Containerise the API and Angular app using Docker. Push images to the container registry. Deploy via docker-compose to the staging server. Run smoke tests post-deployment.",
            Priority = Priority.Medium,
            EstimatedEffort = 4,
            Category = "DevOps",
            Type = TaskType.General,
            Dependencies = new List<string> { "T-202", "T-203" },
            Status = Domain.Enums.TaskStatus.ToDo,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        _tasks.TryAdd(g401.Id, g401);
        _tasks.TryAdd(b301.Id, b301);
        _tasks.TryAdd(g403.Id, g403);
        _tasks.TryAdd(d101.Id, d101);
        _tasks.TryAdd(d102.Id, d102);
        _tasks.TryAdd(t201.Id, t201);
        _tasks.TryAdd(d103.Id, d103);
        _tasks.TryAdd(d104.Id, d104);
        _tasks.TryAdd(t202.Id, t202);
        _tasks.TryAdd(t203.Id, t203);
        _tasks.TryAdd(g402.Id, g402);
    }

    public Task<IEnumerable<TaskItem>> GetAllAsync()
    {
        return Task.FromResult(_tasks.Values.AsEnumerable());
    }

    public Task<IEnumerable<TaskItem>> GetPagedAsync(string? cursor, int pageSize)
    {
        var allTasks = _tasks.Values
            .OrderBy(t => t.CreatedAt)
            .ThenBy(t => t.Id)
            .ToList();

        if (string.IsNullOrWhiteSpace(cursor))
        {
            return Task.FromResult(allTasks.Take(pageSize));
        }

        try
        {
            var decodedBytes = Convert.FromBase64String(cursor);
            var decodedString = System.Text.Encoding.UTF8.GetString(decodedBytes);
            var parts = decodedString.Split('|');
            if (parts.Length == 2)
            {
                var cursorCreatedAt = DateTime.Parse(parts[0], null, System.Globalization.DateTimeStyles.RoundtripKind);
                var cursorId = parts[1];

                var filteredTasks = allTasks
                    .Where(t => t.CreatedAt > cursorCreatedAt || (t.CreatedAt == cursorCreatedAt && string.Compare(t.Id, cursorId) > 0))
                    .Take(pageSize);

                return Task.FromResult(filteredTasks);
            }
        }
        catch
        {
            // Fallback if cursor decoding/parsing fails
        }

        return Task.FromResult(allTasks.Take(pageSize));
    }

    public Task<TaskItem?> GetByIdAsync(string id)
    {
        _tasks.TryGetValue(id, out var task);
        return Task.FromResult(task);
    }

    public Task<TaskItem> AddAsync(TaskItem task)
    {
        if (string.IsNullOrWhiteSpace(task.Id))
        {
            task.Id = GenerateNextId(task.Type);
        }
        _tasks[task.Id] = task;
        return Task.FromResult(task);
    }

    public Task UpdateAsync(TaskItem task)
    {
        _tasks[task.Id] = task;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id)
    {
        _tasks.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    private string GenerateNextId(TaskType type)
    {
        char prefix = type switch
        {
            TaskType.Development => 'D',
            TaskType.Testing => 'T',
            TaskType.Bug => 'B',
            _ => 'G'
        };

        int startNumber = type switch
        {
            TaskType.Development => 101,
            TaskType.Testing => 201,
            TaskType.Bug => 301,
            _ => 401
        };

        string prefixString = $"{prefix}-";
        var existingNumbers = _tasks.Keys
            .Where(id => id.StartsWith(prefixString))
            .Select(id => id.Substring(prefixString.Length))
            .Select(numStr => int.TryParse(numStr, out int num) ? num : 0)
            .Where(num => num > 0)
            .ToList();

        int nextNumber = existingNumbers.Any() ? existingNumbers.Max() + 1 : startNumber;
        return $"{prefixString}{nextNumber}";
    }
}

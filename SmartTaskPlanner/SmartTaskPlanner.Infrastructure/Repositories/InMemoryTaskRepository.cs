using System.Collections.Concurrent;
using SmartTaskPlanner.Application.Interfaces;
using SmartTaskPlanner.Domain.Entities;
using SmartTaskPlanner.Domain.Enums;
using TaskStatus = SmartTaskPlanner.Domain.Enums.TaskStatus;

namespace SmartTaskPlanner.Infrastructure.Repositories;

public class InMemoryTaskRepository : ITaskRepository
{
    private readonly ConcurrentDictionary<string, TaskItem> _tasks = new();

    public InMemoryTaskRepository()
    {
        var g401 = new TaskItem(
            id: "G-401",
            title: "Project Bootstrapping & CI Setup",
            description: "Initialize the Git repository, set up the solution structure, configure CI pipeline (GitHub Actions), and establish code style guidelines (EditorConfig, linting).",
            priority: Priority.High,
            estimatedEffort: 3,
            category: "DevOps",
            type: TaskType.General,
            dependencies: new List<string>(),
            status: TaskStatus.Done,
            createdAt: DateTime.UtcNow.AddDays(-10));

        var b301 = new TaskItem(
            id: "B-301",
            title: "Fix: API returns 500 on empty dependency list",
            description: "When creating a task with an empty dependency array ([]), the API throws a NullReferenceException instead of accepting it as valid input. Reproduces in dev environment.",
            priority: Priority.High,     
            estimatedEffort: 1,
            category: "Backend",
            type: TaskType.Bug,
            dependencies: new List<string>(),
            status: TaskStatus.InProgress,
            createdAt: DateTime.UtcNow.AddDays(-3));

        var g403 = new TaskItem(
            id: "G-403",
            title: "Write API Documentation (Swagger Annotations)",
            description: "Add XML documentation comments to all controller actions and DTOs. Configure Swagger to display descriptions, example payloads, and response schemas.",
            priority: Priority.Low,
            estimatedEffort: 2,
            category: "Documentation",
            type: TaskType.General,
            dependencies: new List<string>(),
            status: TaskStatus.ToDo,
            createdAt: DateTime.UtcNow.AddDays(-8));



        var d101 = new TaskItem(
            id: "D-101",
            title: "Implement Authentication Service (JWT)",
            description: "Build the login and token-refresh endpoints. Integrate ASP.NET Identity. Generate signed JWT tokens with appropriate claims for role-based access.",
            priority: Priority.High,
            estimatedEffort: 8,
            category: "Backend",
            type: TaskType.Development,
            dependencies: new List<string> { "G-401" },
            status: TaskStatus.InProgress,
            createdAt: DateTime.UtcNow.AddDays(-9));

        var d102 = new TaskItem(
            id: "D-102",
            title: "Build Core Task Management API",
            description: "Implement all CRUD endpoints for tasks (/api/tasks). Apply FluentValidation, dependency graph validation, and wire up the execution plan endpoint (/api/tasks/plan).",
            priority: Priority.High,
            estimatedEffort: 6,
            category: "Backend",
            type: TaskType.Development,
            dependencies: new List<string> { "G-401" },
            status: TaskStatus.InProgress,
            createdAt: DateTime.UtcNow.AddDays(-9));

        var t201 = new TaskItem(
            id: "T-201",
            title: "Integration Test: Bootstrapping & Configuration",
            description: "Verify that the application starts correctly, Serilog is configured, Swagger UI is accessible, and CORS headers are present in responses.",
            priority: Priority.Medium,
            estimatedEffort: 2,
            category: "Quality Assurance",
            type: TaskType.Testing,
            dependencies: new List<string> { "G-401" },
            status: TaskStatus.ToDo,
            createdAt: DateTime.UtcNow.AddDays(-8));

        var d103 = new TaskItem(
            id: "D-103",
            title: "Build Login & Registration UI (Angular)",
            description: "Create Angular components for login, registration, and forgot-password flows. Implement JWT interceptor to attach the Bearer token to all subsequent API calls.",
            priority: Priority.High,
            estimatedEffort: 5,
            category: "Frontend",
            type: TaskType.Development,
            dependencies: new List<string> { "D-101" },
            status: TaskStatus.ToDo,
            createdAt: DateTime.UtcNow.AddDays(-7));

        var d104 = new TaskItem(
            id: "D-104",
            title: "Build Task List & Execution Plan UI (Angular)",
            description: "Implement the main task dashboard: task list with filtering, task creation/edit form, task detail view, and the execution plan visualisation panel.",
            priority: Priority.High,
            estimatedEffort: 7,
            category: "Frontend",
            type: TaskType.Development,
            dependencies: new List<string> { "D-102" },
            status: TaskStatus.ToDo,
            createdAt: DateTime.UtcNow.AddDays(-7));

        var t202 = new TaskItem(
            id: "T-202",
            title: "Unit Tests: TaskService & GraphService",
            description: "Write unit tests for GraphService (cycle detection, topological sort, priority ordering) and TaskService (CRUD orchestration, dependency validation). Use xUnit + Moq.",
            priority: Priority.High,
            estimatedEffort: 4,
            category: "Quality Assurance",
            type: TaskType.Testing,
            dependencies: new List<string> { "D-101", "D-102" },
            status: TaskStatus.ToDo,
            createdAt: DateTime.UtcNow.AddDays(-6));

        var t203 = new TaskItem(
            id: "T-203",
            title: "End-to-End Tests: Full User Journey",
            description: "Write E2E tests (Cypress or Playwright) covering: user login → view tasks → create task with dependencies → view execution plan → mark task complete → delete task.",
            priority: Priority.Medium,
            estimatedEffort: 6,
            category: "Quality Assurance",
            type: TaskType.Testing,
            dependencies: new List<string> { "D-103", "D-104" },
            status: TaskStatus.ToDo,
            createdAt: DateTime.UtcNow.AddDays(-4));

        var g402 = new TaskItem(
            id: "G-402",
            title: "Deploy to Staging Environment",
            description: "Containerise the API and Angular app using Docker. Push images to the container registry. Deploy via docker-compose to the staging server. Run smoke tests post-deployment.",
            priority: Priority.Medium,
            estimatedEffort: 4,
            category: "DevOps",
            type: TaskType.General,
            dependencies: new List<string> { "T-202", "T-203" },
            status: TaskStatus.ToDo,
            createdAt: DateTime.UtcNow.AddDays(-2));

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

    public Task<IEnumerable<TaskItem>> GetAllAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_tasks.Values.AsEnumerable());
    }

    public Task<IEnumerable<TaskItem>> GetPagedAsync(string? cursor, int pageSize, string? search = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        
        var query = _tasks.Values.AsEnumerable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t => 
                t.Title.Contains(search, StringComparison.OrdinalIgnoreCase) || 
                t.Id.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (t.Category != null && t.Category.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        var allTasks = query
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
        }

        return Task.FromResult(allTasks.Take(pageSize));
    }

    public Task<TaskItem?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _tasks.TryGetValue(id, out var task);
        return Task.FromResult(task);
    }

    public Task<TaskItem> AddAsync(TaskItem task, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(task.Id))
        {
            task.AssignId(GenerateNextId(task.Type));
        }
        _tasks[task.Id] = task;
        return Task.FromResult(task);
    }

    public Task UpdateAsync(TaskItem task, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _tasks[task.Id] = task;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
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

using SmartTaskPlanner.Domain.Entities;
using SmartTaskPlanner.Domain.Enums;
using SmartTaskPlanner.Infrastructure.Repositories;
using TaskStatus = SmartTaskPlanner.Domain.Enums.TaskStatus;

namespace SmartTaskPlanner.Tests;

/// <summary>
/// Unit tests for <see cref="InMemoryTaskRepository"/>.
/// These tests verify CRUD operations and ID generation logic
/// directly on the in-memory store.
///
/// Covers:
///  - Seed data: tasks exist on construction, correct count
///  - GetAllAsync: returns all tasks
///  - GetByIdAsync: existing ID, non-existing ID
///  - AddAsync: new task added, ID generation by type
///  - UpdateAsync: existing task fields replaced
///  - DeleteAsync: task removed
///  - ID Generation: correct prefix and incrementing numbers per type
/// </summary>
public class InMemoryRepositoryTests
{
    // Fresh repository instance for each test (in-memory, no shared state)
    private InMemoryTaskRepository CreateRepo() => new();

    private static TaskItem MakeTask(TaskType type = TaskType.General,
        string? id = null, string? dependsOn = null) => new(
        id: id ?? string.Empty,
        title: $"Task {type}",
        description: "Test task",
        priority: Priority.Medium,
        estimatedEffort: 2,
        category: "Test",
        type: type,
        dependencies: dependsOn != null ? new List<string> { dependsOn } : new List<string>(),
        status: TaskStatus.ToDo,
        createdAt: DateTime.UtcNow);

   
    // Seed Data
    [Fact]
    public async Task SeedData_OnConstruction_RepositoryIsNotEmpty()
    {
        var repo = CreateRepo();
        var all = (await repo.GetAllAsync()).ToList();
        Assert.NotEmpty(all);
    }

    [Fact]
    public async Task SeedData_OnConstruction_ContainsExpectedTaskIds()
    {
        var repo = CreateRepo();
        var ids = (await repo.GetAllAsync()).Select(t => t.Id).ToList();

        // Verify all known seed IDs are present
        Assert.Contains("G-401", ids);
        Assert.Contains("B-301", ids);
        Assert.Contains("G-403", ids);
        Assert.Contains("D-101", ids);
        Assert.Contains("D-102", ids);
        Assert.Contains("T-201", ids);
        Assert.Contains("D-103", ids);
        Assert.Contains("D-104", ids);
        Assert.Contains("T-202", ids);
        Assert.Contains("T-203", ids);
        Assert.Contains("G-402", ids);
    }

    [Fact]
    public async Task SeedData_OnConstruction_Has11Tasks()
    {
        var repo = CreateRepo();
        var all = await repo.GetAllAsync();
        Assert.Equal(11, all.Count());
    }

    [Fact]
    public async Task SeedData_BugTask_HasHighPriority()
    {
        var repo = CreateRepo();
        var bug = (await repo.GetAllAsync()).First(t => t.Id == "B-301");
        Assert.Equal(Priority.High, bug.Priority);
    }

    [Fact]
    public async Task SeedData_TestingTasks_HaveQualityAssuranceCategory()
    {
        var repo = CreateRepo();
        var testingTasks = (await repo.GetAllAsync())
            .Where(t => t.Type == TaskType.Testing)
            .ToList();

        Assert.NotEmpty(testingTasks);
        Assert.All(testingTasks, t => Assert.Equal("Quality Assurance", t.Category));
    }

   
    // GetAllAsync
    [Fact]
    public async Task GetAllAsync_ReturnsAllTasks()
    {
        var repo = CreateRepo();
        var result = await repo.GetAllAsync();
        Assert.NotNull(result);
    }

   
    // GetByIdAsync
    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsTask()
    {
        var repo = CreateRepo();
        var task = await repo.GetByIdAsync("G-401");

        Assert.NotNull(task);
        Assert.Equal("G-401", task!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var repo = CreateRepo();
        var task = await repo.GetByIdAsync("DOES_NOT_EXIST");
        Assert.Null(task);
    }

   
    // AddAsync

    [Fact]
    public async Task AddAsync_NewTask_IsReturnedByGetAll()
    {
        var repo = CreateRepo();
        var newTask = MakeTask(TaskType.General);
        var added = await repo.AddAsync(newTask);

        var all = await repo.GetAllAsync();
        Assert.Contains(all, t => t.Id == added.Id);
    }

    [Fact]
    public async Task AddAsync_EmptyId_GeneratesId()
    {
        var repo = CreateRepo();
        var task = MakeTask(TaskType.General); // Id = string.Empty
        var added = await repo.AddAsync(task);

        Assert.False(string.IsNullOrWhiteSpace(added.Id));
    }

    [Fact]
    public async Task AddAsync_PreExistingId_UsesProvidedId()
    {
        var repo = CreateRepo();
        var task = MakeTask(TaskType.General, id: "CUSTOM-999");
        var added = await repo.AddAsync(task);

        Assert.Equal("CUSTOM-999", added.Id);
    }

   
    // ID Generation — Prefix and Incrementing
    [Fact]
    public async Task AddAsync_GeneralType_IdStartsWithG()
    {
        var repo = CreateRepo();
        var task = MakeTask(TaskType.General);
        var added = await repo.AddAsync(task);

        Assert.StartsWith("G-", added.Id);
    }

    [Fact]
    public async Task AddAsync_DevelopmentType_IdStartsWithD()
    {
        var repo = CreateRepo();
        var task = MakeTask(TaskType.Development);
        var added = await repo.AddAsync(task);

        Assert.StartsWith("D-", added.Id);
    }

    [Fact]
    public async Task AddAsync_TestingType_IdStartsWithT()
    {
        var repo = CreateRepo();
        var task = MakeTask(TaskType.Testing);
        var added = await repo.AddAsync(task);

        Assert.StartsWith("T-", added.Id);
    }

    [Fact]
    public async Task AddAsync_BugType_IdStartsWithB()
    {
        var repo = CreateRepo();
        var task = MakeTask(TaskType.Bug);
        var added = await repo.AddAsync(task);

        Assert.StartsWith("B-", added.Id);
    }

    [Fact]
    public async Task AddAsync_TwoDevelopmentTasks_IdsAreUnique()
    {
        var repo = CreateRepo();
        var t1 = await repo.AddAsync(MakeTask(TaskType.Development));
        var t2 = await repo.AddAsync(MakeTask(TaskType.Development));

        Assert.NotEqual(t1.Id, t2.Id);
    }

    [Fact]
    public async Task AddAsync_TwoDevelopmentTasks_IdsAreIncremental()
    {
        var repo = CreateRepo();
        var t1 = await repo.AddAsync(MakeTask(TaskType.Development));
        var t2 = await repo.AddAsync(MakeTask(TaskType.Development));

        var num1 = int.Parse(t1.Id.Split('-')[1]);
        var num2 = int.Parse(t2.Id.Split('-')[1]);

        Assert.True(num2 > num1, $"Expected {num2} > {num1}");
    }

   
    // UpdateAsync
    [Fact]
    public async Task UpdateAsync_ExistingTask_ReplacesData()
    {
        var repo = CreateRepo();
        var existing = (await repo.GetByIdAsync("G-401"))!;

        // Use the entity's Update() method — direct property mutation is no longer allowed
        existing.Update(
            title: "Updated Title",
            description: existing.Description,
            priority: Priority.Low,
            estimatedEffort: existing.EstimatedEffort,
            category: existing.Category,
            type: existing.Type,
            status: existing.Status,
            dependencies: existing.Dependencies);
        await repo.UpdateAsync(existing);

        var updated = await repo.GetByIdAsync("G-401");
        Assert.Equal("Updated Title", updated!.Title);
        Assert.Equal(Priority.Low, updated.Priority);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotChangeTotalCount()
    {
        var repo = CreateRepo();
        var countBefore = (await repo.GetAllAsync()).Count();

        var existing = (await repo.GetByIdAsync("G-401"))!;
        existing.Update(
            title: "Changed",
            description: existing.Description,
            priority: existing.Priority,
            estimatedEffort: existing.EstimatedEffort,
            category: existing.Category,
            type: existing.Type,
            status: existing.Status,
            dependencies: existing.Dependencies);
        await repo.UpdateAsync(existing);

        var countAfter = (await repo.GetAllAsync()).Count();
        Assert.Equal(countBefore, countAfter);
    }

   
    // DeleteAsync
    [Fact]
    public async Task DeleteAsync_ExistingId_TaskNoLongerExists()
    {
        var repo = CreateRepo();
        await repo.DeleteAsync("G-403"); // standalone doc task — safe to delete

        var deleted = await repo.GetByIdAsync("G-403");
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_DecrementsCount()
    {
        var repo = CreateRepo();
        var countBefore = (await repo.GetAllAsync()).Count();

        await repo.DeleteAsync("G-403");

        var countAfter = (await repo.GetAllAsync()).Count();
        Assert.Equal(countBefore - 1, countAfter);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_DoesNotThrow()
    {
        var repo = CreateRepo();
        var exception = await Record.ExceptionAsync(() => repo.DeleteAsync("NONEXISTENT"));
        Assert.Null(exception);
    }

   
    // GetPagedAsync (Keyset Pagination)
    [Fact]
    public async Task GetPagedAsync_NullCursor_ReturnsFirstPage()
    {
        var repo = CreateRepo();
        var pageSize = 3;
        var result = (await repo.GetPagedAsync(null, pageSize)).ToList();

        Assert.Equal(pageSize, result.Count);
        
        // Assert first page matches chronological order of seed data (oldest first)
        var allOrdered = (await repo.GetAllAsync()).OrderBy(t => t.CreatedAt).ThenBy(t => t.Id).ToList();
        for (int i = 0; i < pageSize; i++)
        {
            Assert.Equal(allOrdered[i].Id, result[i].Id);
        }
    }

    [Fact]
    public async Task GetPagedAsync_WithValidCursor_ReturnsNextPage()
    {
        var repo = CreateRepo();
        var pageSize = 3;
        
        // Fetch first page
        var page1 = (await repo.GetPagedAsync(null, pageSize)).ToList();
        var lastItem = page1.Last();
        
        // Create cursor for lastItem
        var cursorValue = $"{lastItem.CreatedAt:O}|{lastItem.Id}";
        var cursor = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(cursorValue));

        // Fetch second page
        var page2 = (await repo.GetPagedAsync(cursor, pageSize)).ToList();
        
        Assert.NotEmpty(page2);
        Assert.True(page2.Count <= pageSize);
        
        // Ensure no elements in page2 are in page1 (or chronologically before)
        foreach (var task in page2)
        {
            Assert.True(task.CreatedAt > lastItem.CreatedAt || (task.CreatedAt == lastItem.CreatedAt && string.Compare(task.Id, lastItem.Id) > 0));
        }
    }
}

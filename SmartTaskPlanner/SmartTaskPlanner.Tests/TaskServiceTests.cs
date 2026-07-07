using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartTaskPlanner.Application.DTOs;
using SmartTaskPlanner.Application.Interfaces;
using SmartTaskPlanner.Application.Services;
using SmartTaskPlanner.Domain.Entities;
using SmartTaskPlanner.Domain.Enums;
using SmartTaskPlanner.Domain.Exceptions;
using SmartTaskPlanner.Domain.Factories;
using TaskStatus = SmartTaskPlanner.Domain.Enums.TaskStatus;

namespace SmartTaskPlanner.Tests;

public class TaskServiceTests
{
    // ── Mocks ─────────────────────────────────────────────────────────────────
    private readonly Mock<ITaskRepository> repo = new();
    private readonly Mock<IGraphService> _graphMock = new();
    private readonly Mock<ITaskFactory> _factoryMock = new();

    // System Under Test
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _sut = new TaskService(
            repo.Object,
            _graphMock.Object,
            _factoryMock.Object,
            NullLogger<TaskService>.Instance);
    }

    
    // Helpers
    private static TaskItem MakeEntity(string id, string title = "Sample Task") => new(
        id: id,
        title: title,
        description: "Description",
        priority: Priority.Medium,
        estimatedEffort: 3,
        category: "General",
        type: TaskType.General,
        dependencies: new List<string>(),
        status: TaskStatus.ToDo,
        createdAt: DateTime.UtcNow);

    private static CreateTaskDto MakeCreateDto(string title = "New Task", List<string>? deps = null) =>
        new(title, "Desc", Priority.Medium, 2, "Cat", TaskType.General, deps ?? new List<string>());

    private static UpdateTaskDto MakeUpdateDto(string title = "Updated Task") =>
        new(title, "Updated Desc", Priority.High, 4, "NewCat", TaskType.Development,
            TaskStatus.InProgress, new List<string>());

    
    // GetAllTasksAsync
    [Fact]
    public async Task GetAllTasksAsync_EmptyRepository_ReturnsEmptyList()
    {
        // NOTE: Moq expression trees do not support optional arguments.
        // We must pass It.IsAny<CancellationToken>() explicitly for every mock setup.
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<TaskItem>());

        var result = await _sut.GetAllTasksAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllTasksAsync_MultipleItems_ReturnsMappedDtos()
    {
        var tasks = new[] { MakeEntity("T1", "Task One"), MakeEntity("T2", "Task Two") };
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tasks);

        var result = (await _sut.GetAllTasksAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, dto => dto.Id == "T1" && dto.Title == "Task One");
        Assert.Contains(result, dto => dto.Id == "T2" && dto.Title == "Task Two");
    }

    [Fact]
    public async Task GetAllTasksAsync_RepositoryCalledExactlyOnce()
    {
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Enumerable.Empty<TaskItem>());

        await _sut.GetAllTasksAsync();

        repo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    
    // GetTaskByIdAsync
    [Fact]
    public async Task GetTaskByIdAsync_ExistingId_ReturnsMappedDto()
    {
        var entity = MakeEntity("T1", "Found Task");
        repo.Setup(r => r.GetByIdAsync("T1", It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var result = await _sut.GetTaskByIdAsync("T1");

        Assert.Equal("T1", result.Id);
        Assert.Equal("Found Task", result.Title);
    }

    [Fact]
    public async Task GetTaskByIdAsync_NonExistingId_ThrowsTaskNotFoundException()
    {
        repo.Setup(r => r.GetByIdAsync("GHOST", It.IsAny<CancellationToken>())).ReturnsAsync((TaskItem?)null);

        await Assert.ThrowsAsync<TaskNotFoundException>(() => _sut.GetTaskByIdAsync("GHOST"));
    }

    
    // CreateTaskAsync
    [Fact]
    public async Task CreateTaskAsync_ValidInput_ReturnsCreatedDto()
    {
        var dto = MakeCreateDto("New Feature");
        var createdEntity = MakeEntity("D-101", "New Feature");

        _factoryMock
            .Setup(f => f.Create(dto.Title, dto.Description, dto.Priority,
                dto.EstimatedEffort, dto.Category, dto.Type, dto.Dependencies))
            .Returns(createdEntity);

        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Enumerable.Empty<TaskItem>());
        _graphMock.Setup(g => g.ValidateGraph(It.IsAny<IEnumerable<TaskItem>>(), createdEntity));
        repo.Setup(r => r.AddAsync(createdEntity, It.IsAny<CancellationToken>())).ReturnsAsync(createdEntity);

        var result = await _sut.CreateTaskAsync(dto);

        Assert.Equal("D-101", result.Id);
        Assert.Equal("New Feature", result.Title);
    }

    [Fact]
    public async Task CreateTaskAsync_ValidInput_CallsFactoryAndGraph()
    {
        var dto = MakeCreateDto();
        var entity = MakeEntity("T1");

        _factoryMock
            .Setup(f => f.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Priority>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TaskType>(), It.IsAny<List<string>>()))
            .Returns(entity);

        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Enumerable.Empty<TaskItem>());
        repo.Setup(r => r.AddAsync(entity, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        await _sut.CreateTaskAsync(dto);

        _factoryMock.Verify(f => f.Create(
            dto.Title, dto.Description, dto.Priority, dto.EstimatedEffort,
            dto.Category, dto.Type, dto.Dependencies), Times.Once);

        _graphMock.Verify(g => g.ValidateGraph(
            It.IsAny<IEnumerable<TaskItem>>(), entity), Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_GraphValidationFails_ThrowsAndDoesNotSave()
    {
        var dto = MakeCreateDto();
        var entity = MakeEntity("T1");

        _factoryMock
            .Setup(f => f.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Priority>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TaskType>(), It.IsAny<List<string>>()))
            .Returns(entity);

        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Enumerable.Empty<TaskItem>());
        _graphMock
            .Setup(g => g.ValidateGraph(It.IsAny<IEnumerable<TaskItem>>(), entity))
            .Throws(new CircularDependencyException("Cycle detected"));

        await Assert.ThrowsAsync<CircularDependencyException>(() => _sut.CreateTaskAsync(dto));

        // AddAsync must NOT be called when validation fails
        repo.Verify(r => r.AddAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    
    // UpdateTaskAsync
    [Fact]
    public async Task UpdateTaskAsync_ExistingId_UpdatesAndCallsGraph()
    {
        var entity = MakeEntity("T1");
        var dto = MakeUpdateDto();

        repo.Setup(r => r.GetByIdAsync("T1", It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { entity });
        repo.Setup(r => r.UpdateAsync(entity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _sut.UpdateTaskAsync("T1", dto);

        // Verify business rules are re-applied and graph validation runs
        _factoryMock.Verify(f => f.ApplyBusinessRules(entity), Times.Once);
        _graphMock.Verify(g => g.ValidateGraph(
            It.IsAny<IEnumerable<TaskItem>>(), It.IsAny<TaskItem>()), Times.Once);
        repo.Verify(r => r.UpdateAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskAsync_BugTaskWithLowPriority_BusinessRulesPreventBypass()
    {
        // Arrange: entity is a Bug task; user attempts to update it with Low priority
        var entity = new TaskItem(
            id: "B-301",
            title: "Bug Task",
            description: "A bug",
            priority: Priority.High,
            estimatedEffort: 1,
            category: "Backend",
            type: TaskType.Bug,
            dependencies: new List<string>(),
            status: TaskStatus.ToDo,
            createdAt: DateTime.UtcNow);

        var dto = new UpdateTaskDto(
            "Bug Task", "A bug", Priority.Low, 1, "Backend",
            TaskType.Bug, TaskStatus.ToDo, new List<string>());

        repo.Setup(r => r.GetByIdAsync("B-301", It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { entity });
        repo.Setup(r => r.UpdateAsync(entity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Make the mock actually apply the real business rule so we can assert the outcome
        _factoryMock
            .Setup(f => f.ApplyBusinessRules(It.IsAny<TaskItem>()))
            .Callback<TaskItem>(t =>
            {
                // Use the entity's Update() method to simulate the factory's behaviour
                if (t.Type == TaskType.Bug)
                    t.Update(t.Title, t.Description, Priority.High, t.EstimatedEffort,
                             t.Category, t.Type, t.Status, t.Dependencies);
            });

        // Act
        await _sut.UpdateTaskAsync("B-301", dto);

        // Assert: priority must be High despite the DTO carrying Low
        Assert.Equal(Priority.High, entity.Priority);
        _factoryMock.Verify(f => f.ApplyBusinessRules(entity), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskAsync_NonExistingId_ThrowsTaskNotFoundException()
    {
        repo.Setup(r => r.GetByIdAsync("GHOST", It.IsAny<CancellationToken>())).ReturnsAsync((TaskItem?)null);

        await Assert.ThrowsAsync<TaskNotFoundException>(() =>
            _sut.UpdateTaskAsync("GHOST", MakeUpdateDto()));
    }

    [Fact]
    public async Task UpdateTaskAsync_AppliesAllFieldsFromDto()
    {
        var entity = MakeEntity("T1", "Old Title");
        var dto = MakeUpdateDto("New Title");

        repo.Setup(r => r.GetByIdAsync("T1", It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { entity });
        repo.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _sut.UpdateTaskAsync("T1", dto);

        // Verify the entity was mutated with dto values before saving
        Assert.Equal("New Title", entity.Title);
        Assert.Equal(Priority.High, entity.Priority);
        Assert.Equal(TaskStatus.InProgress, entity.Status);
    }

    [Fact]
    public async Task UpdateTaskAsync_GraphValidationFails_ThrowsAndDoesNotPersist()
    {
        var entity = MakeEntity("T1");
        var dto = MakeUpdateDto();

        repo.Setup(r => r.GetByIdAsync("T1", It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { entity });
        _graphMock
            .Setup(g => g.ValidateGraph(It.IsAny<IEnumerable<TaskItem>>(), It.IsAny<TaskItem>()))
            .Throws(new CircularDependencyException("Cycle detected"));

        await Assert.ThrowsAsync<CircularDependencyException>(() =>
            _sut.UpdateTaskAsync("T1", dto));

        repo.Verify(r => r.UpdateAsync(It.IsAny<TaskItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    
    // DeleteTaskAsync
    [Fact]
    public async Task DeleteTaskAsync_ExistingIdNoDependents_DeletesSuccessfully()
    {
        var entity = MakeEntity("T1");
        repo.Setup(r => r.GetByIdAsync("T1", It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { entity }); // only itself, no other task depends on it
        repo.Setup(r => r.DeleteAsync("T1", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        await _sut.DeleteTaskAsync("T1");

        repo.Verify(r => r.DeleteAsync("T1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTaskAsync_NonExistingId_ThrowsTaskNotFoundException()
    {
        repo.Setup(r => r.GetByIdAsync("GHOST", It.IsAny<CancellationToken>())).ReturnsAsync((TaskItem?)null);

        await Assert.ThrowsAsync<TaskNotFoundException>(() => _sut.DeleteTaskAsync("GHOST"));
    }

    [Fact]
    public async Task DeleteTaskAsync_OtherTaskDependsOnIt_ThrowsDomainException()
    {
        var target = MakeEntity("A");
        var dependent = new TaskItem(
            id: "B",
            title: "B depends on A",
            description: string.Empty,
            priority: Priority.Low,
            estimatedEffort: 1,
            category: "Test",
            type: TaskType.General,
            dependencies: new List<string> { "A" }, // B depends on A
            status: TaskStatus.ToDo,
            createdAt: DateTime.UtcNow);

        repo.Setup(r => r.GetByIdAsync("A", It.IsAny<CancellationToken>())).ReturnsAsync(target);
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { target, dependent });

        await Assert.ThrowsAsync<DomainException>(() => _sut.DeleteTaskAsync("A"));

        repo.Verify(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    
    // GenerateExecutionPlanAsync
    [Fact]
    public async Task GenerateExecutionPlanAsync_DelegatesToGraphService()
    {
        var tasks = new[] { MakeEntity("A"), MakeEntity("B") };
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(tasks);
        _graphMock.Setup(g => g.GenerateExecutionPlan(tasks)).Returns(tasks);

        var result = (await _sut.GenerateExecutionPlanAsync()).ToList();

        _graphMock.Verify(g => g.GenerateExecutionPlan(tasks), Times.Once);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GenerateExecutionPlanAsync_ReturnsMappedDtos()
    {
        var a = MakeEntity("A", "Alpha");
        var b = MakeEntity("B", "Beta");
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { a, b });
        _graphMock.Setup(g => g.GenerateExecutionPlan(It.IsAny<IEnumerable<TaskItem>>()))
            .Returns(new[] { a, b }); // already ordered

        var result = (await _sut.GenerateExecutionPlanAsync()).ToList();

        Assert.Equal("A", result[0].Id);
        Assert.Equal("Alpha", result[0].Title);
        Assert.Equal("B", result[1].Id);
    }

    
    // GetPagedTasksAsync
    [Fact]
    public async Task GetPagedTasksAsync_FirstPageWithoutCursor_CallsRepositoryAndReturnsMappedResponse()
    {
        var tasks = new[] { MakeEntity("T1"), MakeEntity("T2") };
        repo.Setup(r => r.GetPagedAsync(null, 3, It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(tasks);

        var result = await _sut.GetPagedTasksAsync(null, 2);

        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
        Assert.False(result.HasNext);
        Assert.Null(result.NextCursor);
        repo.Verify(r => r.GetPagedAsync(null, 3, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPagedTasksAsync_HasMoreItems_ReturnsTrueHasNextAndNextCursor()
    {
        // Request page size of 2, repository receives size 3, returns 3 items
        var tasks = new[] { MakeEntity("T1"), MakeEntity("T2"), MakeEntity("T3") };
        repo.Setup(r => r.GetPagedAsync(null, 3, It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(tasks);

        var result = await _sut.GetPagedTasksAsync(null, 2);

        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count());
        Assert.True(result.HasNext);
    }
}

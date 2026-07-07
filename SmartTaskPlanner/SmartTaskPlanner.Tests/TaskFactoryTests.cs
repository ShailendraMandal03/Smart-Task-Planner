using SmartTaskPlanner.Domain.Entities;
using SmartTaskPlanner.Domain.Enums;
using SmartTaskPlanner.Domain.Factories;
using DomainTaskFactory = SmartTaskPlanner.Domain.Factories.TaskFactory;
using TaskStatus = SmartTaskPlanner.Domain.Enums.TaskStatus;

namespace SmartTaskPlanner.Tests;

public class TaskFactoryTests
{
    private readonly ITaskFactory _sut = new DomainTaskFactory();

   
    // General Task
    [Fact]
    public void Create_GeneralTask_PreservesAllProvidedValues()
    {
        var task = _sut.Create("My Task", "Desc", Priority.Low, 5, "Ops", TaskType.General,
            new List<string> { "G-401" });

        Assert.Equal("My Task", task.Title);
        Assert.Equal("Desc", task.Description);
        Assert.Equal(Priority.Low, task.Priority);
        Assert.Equal(5, task.EstimatedEffort);
        Assert.Equal("Ops", task.Category);
        Assert.Equal(TaskType.General, task.Type);
        Assert.Contains("G-401", task.Dependencies);
    }

    [Fact]
    public void Create_AnyTask_DefaultStatusIsToDo()
    {
        var task = _sut.Create("T", "D", Priority.Medium, 1, "C", TaskType.General, new List<string>());
        Assert.Equal(TaskStatus.ToDo, task.Status);
    }

    [Fact]
    public void Create_AnyTask_CreatedAtIsSetToNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var task = _sut.Create("T", "D", Priority.Medium, 1, "C", TaskType.General, new List<string>());
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(task.CreatedAt, before, after);
    }

    [Fact]
    public void Create_NullDependencies_DefaultsToEmptyList()
    {
        // Factory should guard against null dependencies list
        var task = _sut.Create("T", "D", Priority.Medium, 1, "C", TaskType.General, null!);
        Assert.NotNull(task.Dependencies);
        Assert.Empty(task.Dependencies);
    }

   
    // Bug Task — Priority Override
    [Fact]
    public void Create_BugTask_PriorityOverriddenToHigh_WhenInputIsLow()
    {
        var task = _sut.Create("Critical Bug", "Desc", Priority.Low, 1, "Backend",
            TaskType.Bug, new List<string>());

        Assert.Equal(Priority.High, task.Priority);
    }

    [Fact]
    public void Create_BugTask_PriorityOverriddenToHigh_WhenInputIsMedium()
    {
        var task = _sut.Create("Bug", "Desc", Priority.Medium, 1, "Frontend",
            TaskType.Bug, new List<string>());

        Assert.Equal(Priority.High, task.Priority);
    }

    [Fact]
    public void Create_BugTask_PriorityRemainsHigh_WhenInputIsAlreadyHigh()
    {
        var task = _sut.Create("Bug", "Desc", Priority.High, 1, "Frontend",
            TaskType.Bug, new List<string>());

        Assert.Equal(Priority.High, task.Priority);
    }

    [Fact]
    public void Create_BugTask_OtherPropertiesNotAffected()
    {
        var task = _sut.Create("Bug Title", "Bug Desc", Priority.Low, 2, "QA",
            TaskType.Bug, new List<string>());

        Assert.Equal("Bug Title", task.Title);
        Assert.Equal("Bug Desc", task.Description);
        Assert.Equal(2, task.EstimatedEffort);
        Assert.Equal("QA", task.Category);
        Assert.Equal(TaskType.Bug, task.Type);
    }

   
    // Testing Task — Category Default
    [Fact]
    public void Create_TestingTask_NullCategory_DefaultsToQualityAssurance()
    {
        var task = _sut.Create("Test Suite", "Desc", Priority.Medium, 2, null!,
            TaskType.Testing, new List<string>());

        Assert.Equal("Quality Assurance", task.Category);
    }

    [Fact]
    public void Create_TestingTask_EmptyCategory_DefaultsToQualityAssurance()
    {
        var task = _sut.Create("Test Suite", "Desc", Priority.Medium, 2, "",
            TaskType.Testing, new List<string>());

        Assert.Equal("Quality Assurance", task.Category);
    }

    [Fact]
    public void Create_TestingTask_WhitespaceCategory_DefaultsToQualityAssurance()
    {
        var task = _sut.Create("Test Suite", "Desc", Priority.Medium, 2, "   ",
            TaskType.Testing, new List<string>());

        Assert.Equal("Quality Assurance", task.Category);
    }

    [Fact]
    public void Create_TestingTask_ProvidedCategory_PreservesCategory()
    {
        var task = _sut.Create("Test Suite", "Desc", Priority.Medium, 2, "Integration",
            TaskType.Testing, new List<string>());

        Assert.Equal("Integration", task.Category);
    }

    [Fact]
    public void Create_TestingTask_PriorityNotOverridden()
    {
        var task = _sut.Create("Test Suite", "Desc", Priority.Low, 2, "",
            TaskType.Testing, new List<string>());

        // Testing tasks do NOT override priority — only Bug tasks do
        Assert.Equal(Priority.Low, task.Priority);
    }

   
    // Development Task — Minimum Effort
    [Fact]
    public void Create_DevelopmentTask_ZeroEffort_SetsToMinimumOne()
    {
        var task = _sut.Create("Feature", "Desc", Priority.High, 0, "Backend",
            TaskType.Development, new List<string>());

        Assert.Equal(1, task.EstimatedEffort);
    }

    [Fact]
    public void Create_DevelopmentTask_NegativeEffort_SetsToMinimumOne()
    {
        var task = _sut.Create("Feature", "Desc", Priority.High, -5, "Backend",
            TaskType.Development, new List<string>());

        Assert.Equal(1, task.EstimatedEffort);
    }

    [Fact]
    public void Create_DevelopmentTask_PositiveEffort_PreservesValue()
    {
        var task = _sut.Create("Feature", "Desc", Priority.High, 8, "Backend",
            TaskType.Development, new List<string>());

        Assert.Equal(8, task.EstimatedEffort);
    }

    [Fact]
    public void Create_DevelopmentTask_PriorityNotOverridden()
    {
        var task = _sut.Create("Feature", "Desc", Priority.Low, 3, "Backend",
            TaskType.Development, new List<string>());

        // Development tasks do NOT override priority
        Assert.Equal(Priority.Low, task.Priority);
    }

   
    // Id Handling
    [Fact]
    public void Create_AnyTask_IdIsEmpty_LetRepositoryAssignIt()
    {
        // Factory should leave Id as empty string;
        // the repository's AddAsync is responsible for generating IDs.
        var task = _sut.Create("T", "D", Priority.Medium, 1, "C", TaskType.General, new List<string>());
        Assert.Equal(string.Empty, task.Id);
    }
}

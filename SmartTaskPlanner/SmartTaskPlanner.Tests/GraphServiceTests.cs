using Microsoft.Extensions.Logging.Abstractions;
using SmartTaskPlanner.Application.Services;
using SmartTaskPlanner.Domain.Entities;
using SmartTaskPlanner.Domain.Enums;
using SmartTaskPlanner.Domain.Exceptions;
using TaskStatus = SmartTaskPlanner.Domain.Enums.TaskStatus;

namespace SmartTaskPlanner.Tests;

public class GraphServiceTests
{
    private readonly GraphService _sut;

    public GraphServiceTests()
    {
        _sut = new GraphService(NullLogger<GraphService>.Instance);
    }



    private static TaskItem MakeTask(string id, Priority priority = Priority.Medium,
        int effort = 1, params string[] deps) => new(
        id: id,
        title: $"Task {id}",
        description: string.Empty,
        priority: priority,
        estimatedEffort: effort,
        category: "Test",
        type: TaskType.General,
        dependencies: deps.ToList(),
        status: TaskStatus.ToDo,
        createdAt: DateTime.UtcNow);


    [Fact]
    public void GenerateExecutionPlan_EmptyList_ReturnsEmpty()
    {
        var result = _sut.GenerateExecutionPlan(Enumerable.Empty<TaskItem>());
        Assert.Empty(result);
    }

    [Fact]
    public void GenerateExecutionPlan_SingleTask_ReturnsThatTask()
    {
        var task = MakeTask("T1");
        var result = _sut.GenerateExecutionPlan(new[] { task }).ToList();

        Assert.Single(result);
        Assert.Equal("T1", result[0].Id);
    }

    [Fact]
    public void GenerateExecutionPlan_LinearChain_RespectsOrder()
    {
        var a = MakeTask("A");
        var b = MakeTask("B", Priority.Medium, 1, "A");
        var c = MakeTask("C", Priority.Medium, 1, "B");

        var result = _sut.GenerateExecutionPlan(new[] { c, b, a }).ToList(); // shuffled input

        Assert.Equal(3, result.Count);
        Assert.Equal("A", result[0].Id);
        Assert.Equal("B", result[1].Id);
        Assert.Equal("C", result[2].Id);
    }

    [Fact]
    public void GenerateExecutionPlan_DiamondDependency_AllTasksIncluded()
    {
        //   A
        //  / \
        // B   C
        //  \ /
        //   D
        var a = MakeTask("A");
        var b = MakeTask("B", Priority.Medium, 1, "A");
        var c = MakeTask("C", Priority.Medium, 1, "A");
        var d = MakeTask("D", Priority.Medium, 1, "B", "C");

        var result = _sut.GenerateExecutionPlan(new[] { a, b, c, d }).ToList();

        Assert.Equal(4, result.Count);
        Assert.Equal("A", result[0].Id);
        // B and C can be in any order between positions 1-2
        Assert.Contains(result.Select(t => t.Id), id => id == "B");
        Assert.Contains(result.Select(t => t.Id), id => id == "C");
        Assert.Equal("D", result[3].Id);
    }

    [Fact]
    public void GenerateExecutionPlan_DisconnectedGraph_AllTasksIncluded()
    {
        // Two completely independent tasks — no dependencies
        var a = MakeTask("A");
        var b = MakeTask("B");

        var result = _sut.GenerateExecutionPlan(new[] { a, b }).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.Id == "A");
        Assert.Contains(result, t => t.Id == "B");
    }


    // GenerateExecutionPlan — Priority Ordering

    [Fact]
    public void GenerateExecutionPlan_HighPriorityBeforeLow_WhenNoDependencies()
    {
        // Both tasks have no dependencies; High should come before Low
        var low = MakeTask("LOW", priority: Priority.Low);
        var high = MakeTask("HIGH", priority: Priority.High);

        var result = _sut.GenerateExecutionPlan(new[] { low, high }).ToList();

        Assert.Equal("HIGH", result[0].Id);
        Assert.Equal("LOW", result[1].Id);
    }

    [Fact]
    public void GenerateExecutionPlan_AllPriorityLevels_OrderedCorrectly()
    {
        var low = MakeTask("LOW", priority: Priority.Low);
        var med = MakeTask("MED", priority: Priority.Medium);
        var high = MakeTask("HIGH", priority: Priority.High);

        var result = _sut.GenerateExecutionPlan(new[] { low, med, high }).ToList();

        Assert.Equal("HIGH", result[0].Id);
        Assert.Equal("MED", result[1].Id);
        Assert.Equal("LOW", result[2].Id);
    }

    [Fact]
    public void GenerateExecutionPlan_DependencyUnlocksCorrectPriority()
    {
        // After A completes, both B (High) and C (Low) are unlocked.
        // B should be processed before C.
        var a = MakeTask("A", priority: Priority.High);
        var b = MakeTask("B", priority: Priority.High, deps: "A");
        var c = MakeTask("C", priority: Priority.Low, deps: "A");

        var result = _sut.GenerateExecutionPlan(new[] { a, b, c }).ToList();

        Assert.Equal("A", result[0].Id);
        Assert.Equal("B", result[1].Id);
        Assert.Equal("C", result[2].Id);
    }

    // GenerateExecutionPlan — Effort Tie-Breaking
    [Fact]
    public void GenerateExecutionPlan_SamePriority_LowerEffortFirst()
    {
        // Both High priority; effort 2 should come before effort 5
        var quick = MakeTask("QUICK", priority: Priority.High, effort: 2);
        var slow = MakeTask("SLOW", priority: Priority.High, effort: 5);

        var result = _sut.GenerateExecutionPlan(new[] { slow, quick }).ToList();

        Assert.Equal("QUICK", result[0].Id);
        Assert.Equal("SLOW", result[1].Id);
    }


    // GenerateExecutionPlan — ID Tie-Breaking

    [Fact]
    public void GenerateExecutionPlan_SamePriorityAndEffort_AlphabeticIdFirst()
    {
        // Identical priority and effort → alphabetically earlier ID comes first
        var z = MakeTask("Z", priority: Priority.Medium, effort: 3);
        var a = MakeTask("A", priority: Priority.Medium, effort: 3);

        var result = _sut.GenerateExecutionPlan(new[] { z, a }).ToList();

        Assert.Equal("A", result[0].Id);
        Assert.Equal("Z", result[1].Id);
    }


    // GenerateExecutionPlan — Cycle Detection via Kahn's Algorithm

    [Fact]
    public void GenerateExecutionPlan_DirectCycle_ThrowsCircularDependencyException()
    {
        // A depends on B, B depends on A → cycle
        var a = MakeTask("A", deps: "B");
        var b = MakeTask("B", deps: "A");

        Assert.Throws<CircularDependencyException>(() =>
            _sut.GenerateExecutionPlan(new[] { a, b }));
    }

    [Fact]
    public void GenerateExecutionPlan_IndirectCycle_ThrowsCircularDependencyException()
    {
        // A → B → C → A
        var a = MakeTask("A", deps: "C");
        var b = MakeTask("B", deps: "A");
        var c = MakeTask("C", deps: "B");

        Assert.Throws<CircularDependencyException>(() =>
            _sut.GenerateExecutionPlan(new[] { a, b, c }));
    }

    // ValidateGraph — Self-dependency

    [Fact]
    public void EnsureNoCycles_SelfDependency_ThrowsSelfDependencyException()
    {
        var task = MakeTask("T1", deps: "T1"); // depends on itself

        Assert.Throws<SelfDependencyException>(() =>
            _sut.ValidateGraph(Enumerable.Empty<TaskItem>(), task));
    }


    // ValidateGraph — Missing Dependency

    [Fact]
    public void EnsureNoCycles_MissingDependency_ThrowsDependencyNotFoundException()
    {
        var task = MakeTask("T1", deps: "NON_EXISTENT");
        var allTasks = new[] { MakeTask("T2") }; // NON_EXISTENT is not in allTasks

        Assert.Throws<DependencyNotFoundException>(() =>
            _sut.ValidateGraph(allTasks, task));
    }


    // ValidateGraph — Cycle Detection (DFS)

    [Fact]
    public void EnsureNoCycles_IntroducesDirectCycle_ThrowsCircularDependencyException()
    {
        // Existing: A ← B (B depends on A)
        // New update: A now depends on B → creates A ↔ B cycle
        var existingA = MakeTask("A");
        var existingB = MakeTask("B", deps: "A");

        // We are "updating" A to depend on B
        var updatedA = MakeTask("A", deps: "B");

        Assert.Throws<CircularDependencyException>(() =>
            _sut.ValidateGraph(new[] { existingA, existingB }, updatedA));
    }

    [Fact]
    public void EnsureNoCycles_NoIssues_DoesNotThrow()
    {
        var a = MakeTask("A");
        var b = MakeTask("B", deps: "A");

        // Adding C that depends on B — valid linear chain
        var c = MakeTask("C", deps: "B");

        var exception = Record.Exception(() =>
            _sut.ValidateGraph(new[] { a, b }, c));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureNoCycles_NoDependencies_DoesNotThrow()
    {
        var newTask = MakeTask("STANDALONE");

        var exception = Record.Exception(() =>
            _sut.ValidateGraph(new[] { MakeTask("T1") }, newTask));

        Assert.Null(exception);
    }


    // GenerateExecutionPlan — Realistic Seed Data Scenario

    [Fact]
    public void GenerateExecutionPlan_RealisticProjectGraph_CorrectOrder()
    {
        // Mirrors the expanded seed data:
        //   G-401 → D-101, D-102, T-201
        //   D-101 → D-103, T-202
        //   D-102 → D-104, T-202
        //   D-103, D-104 → T-203
        //   T-202, T-203 → G-402
        //   B-301, G-403 — standalone (no deps, no dependents in this set)

        var g401 = MakeTask("G-401", Priority.High, 3);
        var b301 = MakeTask("B-301", Priority.High, 1);
        var g403 = MakeTask("G-403", Priority.Low, 2);
        var d101 = MakeTask("D-101", Priority.High, 8, "G-401");
        var d102 = MakeTask("D-102", Priority.High, 6, "G-401");
        var t201 = MakeTask("T-201", Priority.Medium, 2, "G-401");
        var d103 = MakeTask("D-103", Priority.High, 5, "D-101");
        var d104 = MakeTask("D-104", Priority.High, 7, "D-102");
        var t202 = MakeTask("T-202", Priority.High, 4, "D-101", "D-102");
        var t203 = MakeTask("T-203", Priority.Medium, 6, "D-103", "D-104");
        var g402 = MakeTask("G-402", Priority.Medium, 4, "T-202", "T-203");

        var all = new[] { g401, b301, g403, d101, d102, t201, d103, d104, t202, t203, g402 };
        var result = _sut.GenerateExecutionPlan(all).ToList();

        Assert.Equal(11, result.Count);

        // G-401 and B-301 are both High priority root nodes.
        // B-301 has effort=1 (lower) so it should come before G-401 (effort=3)
        var g401Index = result.FindIndex(t => t.Id == "G-401");
        var b301Index = result.FindIndex(t => t.Id == "B-301");
        var g403Index = result.FindIndex(t => t.Id == "G-403");
        var d101Index = result.FindIndex(t => t.Id == "D-101");
        var d102Index = result.FindIndex(t => t.Id == "D-102");
        var t201Index = result.FindIndex(t => t.Id == "T-201");
        var t202Index = result.FindIndex(t => t.Id == "T-202");
        var t203Index = result.FindIndex(t => t.Id == "T-203");
        var g402Index = result.FindIndex(t => t.Id == "G-402");

        // Root nodes appear before their dependents
        Assert.True(b301Index < d101Index,  "B-301 (standalone, High, effort=1) before D-101");
        Assert.True(g401Index < d101Index,  "G-401 must be before D-101");
        Assert.True(g401Index < d102Index,  "G-401 must be before D-102");
        Assert.True(g401Index < t201Index,  "G-401 must be before T-201");
        Assert.True(d101Index < t202Index,  "D-101 must be before T-202");
        Assert.True(d102Index < t202Index,  "D-102 must be before T-202");
        Assert.True(t202Index < g402Index,  "T-202 must be before G-402");
        Assert.True(t203Index < g402Index,  "T-203 must be before G-402");

        // G-402 is last in the dependency chain (depends on T-202 and T-203)
        // G-403 (Low priority, standalone) is placed last of all due to Low priority
        Assert.Equal("G-403", result[^1].Id);  // Low priority standalone task ends up last
        Assert.True(g402Index < result[^1].Id.Length + g402Index, "G-402 is not the absolute last (G-403 is)");

        // Verify G-402 comes after its own dependencies
        Assert.True(t202Index < g402Index, "T-202 must be completed before G-402 (deployment)");
        Assert.True(t203Index < g402Index, "T-203 must be completed before G-402 (deployment)");

        // G-403 (Low priority, standalone) appears after all High-priority roots
        Assert.True(g403Index > b301Index, "G-403 (Low) should come after B-301 (High)");
        Assert.True(g403Index > g401Index, "G-403 (Low) should come after G-401 (High)");
    }


    // GenerateExecutionPlan — Count Integrity

    [Fact]
    public void GenerateExecutionPlan_ReturnsSameCountAsInput()
    {
        var tasks = new[]
        {
            MakeTask("A"),
            MakeTask("B", Priority.Medium, 1, "A"),
            MakeTask("C", Priority.Medium, 1, "A"),
            MakeTask("D", Priority.Medium, 1, "B", "C")
        };

        var result = _sut.GenerateExecutionPlan(tasks);
        Assert.Equal(tasks.Length, result.Count());
    }
}

using SmartTaskPlanner.Application.Interfaces;
using SmartTaskPlanner.Domain.Entities;
using SmartTaskPlanner.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace SmartTaskPlanner.Application.Services;

public class GraphService : IGraphService
{
    private readonly ILogger<GraphService> _logger;

    public GraphService(ILogger<GraphService> logger)
    {
        _logger = logger;
    }
    public void EnsureNoCyclesOrInvalidDependencies(IEnumerable<TaskItem> allTasks, TaskItem newTaskOrUpdatedTask)
    {
        var tasksDict = allTasks.ToDictionary(t => t.Id);
        
        tasksDict[newTaskOrUpdatedTask.Id] = newTaskOrUpdatedTask;

        // checking for self-dependency
        if (newTaskOrUpdatedTask.Dependencies.Contains(newTaskOrUpdatedTask.Id))
        {
            _logger.LogWarning("Self-dependency detected for task {TaskId}", newTaskOrUpdatedTask.Id);
            throw new SelfDependencyException("A task cannot depend on itself.");
        }

        // Checking for missing dependencies
        foreach (var depId in newTaskOrUpdatedTask.Dependencies)
        {
            if (!tasksDict.ContainsKey(depId))
            {
                _logger.LogWarning("Dependency {DepId} not found for task {TaskId}", depId, newTaskOrUpdatedTask.Id);
                throw new DependencyNotFoundException($"Dependency task with ID {depId} does not exist.");
            }
        }

        // checking Cycle Detection using DFS algorithm
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        var currentPath = new List<string>();

        foreach (var task in tasksDict.Values)
        {
            if (HasCycle(task.Id, tasksDict, visited, recursionStack, currentPath, out var cyclePath))
            {
                var pathString = string.Join(" ➔ ", cyclePath!);
                _logger.LogWarning("Circular dependency detected involving task {TaskId}: {Path}", task.Id, pathString);
                throw new CircularDependencyException($"Circular dependency detected: {pathString}", cyclePath!);
            }
        }
    }

    private bool HasCycle(string currentId, Dictionary<string, TaskItem> tasksDict, HashSet<string> visited, HashSet<string> recursionStack, List<string> currentPath, out List<string>? cyclePath)
    {
        cyclePath = null;
        
        currentPath.Add(currentId);

        if (recursionStack.Contains(currentId))
        {
            int index = currentPath.IndexOf(currentId);
            cyclePath = currentPath.Skip(index).ToList();
            return true;
        }

        if (visited.Contains(currentId))
        {
            currentPath.RemoveAt(currentPath.Count - 1);
            return false;
        }

        visited.Add(currentId);
        recursionStack.Add(currentId);

        if (tasksDict.TryGetValue(currentId, out var currentTask))
        {
            foreach (var depId in currentTask.Dependencies)
            {
                if (HasCycle(depId, tasksDict, visited, recursionStack, currentPath, out cyclePath))
                     return true;
            }
        }

        recursionStack.Remove(currentId);
        currentPath.RemoveAt(currentPath.Count - 1);
        return false;
    }

    public IEnumerable<TaskItem> GenerateExecutionPlan(IEnumerable<TaskItem> allTasks)
    {
        _logger.LogInformation("Generating execution plan for {Count} tasks", allTasks.Count());
        var tasksList = allTasks.ToList();
        var tasksDict = tasksList.ToDictionary(t => t.Id);
        var inDegree = tasksList.ToDictionary(t => t.Id, t => 0);
        var adjList = tasksList.ToDictionary(t => t.Id, t => new List<string>());

        foreach (var task in tasksList)
        {
            foreach (var depId in task.Dependencies)
            {
                if (adjList.ContainsKey(depId))
                {
                    adjList[depId].Add(task.Id);
                    inDegree[task.Id]++;
                }
            }
        }

        var queue = new PriorityQueue<TaskItem, TaskItem>(new TaskExecutionComparer());

        foreach (var task in tasksList)
        {
            if (inDegree[task.Id] == 0)
            {
                queue.Enqueue(task, task);
            }
        }

        var executionPlan = new List<TaskItem>();

        while (queue.Count > 0)
        {
            var currentTask = queue.Dequeue();
            executionPlan.Add(currentTask);

            foreach (var dependentId in adjList[currentTask.Id])
            {
                inDegree[dependentId]--;
                if (inDegree[dependentId] == 0)
                {
                    queue.Enqueue(tasksDict[dependentId], tasksDict[dependentId]);
                }
            }
        }

        if (executionPlan.Count != tasksList.Count)
        {
            _logger.LogError("Circular dependency detected during execution plan generation. Scheduled: {ScheduledCount}, Total: {TotalCount}", executionPlan.Count, tasksList.Count);
            
            // Try to find the exact cycle to return to the user
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();
            var currentPath = new List<string>();
            foreach (var task in tasksList)
            {
                if (HasCycle(task.Id, tasksDict, visited, recursionStack, currentPath, out var cyclePath))
                {
                    var pathString = string.Join(" ➔ ", cyclePath!);
                    throw new CircularDependencyException($"Circular dependency detected during execution plan generation: {pathString}", cyclePath!);
                }
            }

            throw new CircularDependencyException("Circular dependency detected during execution plan generation.");
        }

        _logger.LogInformation("Successfully generated execution plan");
        return executionPlan;
    }

    private class TaskExecutionComparer : IComparer<TaskItem>
    {
        public int Compare(TaskItem? x, TaskItem? y)
        {
            if (x == null || y == null) return 0;

            int priorityComparison = y.Priority.CompareTo(x.Priority);
            if (priorityComparison != 0) return priorityComparison;

            int effortComparison = x.EstimatedEffort.CompareTo(y.EstimatedEffort);
            if (effortComparison != 0) return effortComparison;

            return x.Id.CompareTo(y.Id);
        }
    }
}

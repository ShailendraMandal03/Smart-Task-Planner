using SmartTaskPlanner.Application.Interfaces;
using SmartTaskPlanner.Domain.Entities;
using SmartTaskPlanner.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace SmartTaskPlanner.Application.Services;

public class GraphService : IGraphService
{
    private readonly ILogger<GraphService> _logger;
    private readonly ITaskRepository _repository;

    public GraphService(ILogger<GraphService> logger, ITaskRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public async Task ValidateGraphAsync(TaskItem newTaskOrUpdatedTask, CancellationToken ct = default)
    {
        if (newTaskOrUpdatedTask.Dependencies.Contains(newTaskOrUpdatedTask.Id))
        {
            _logger.LogWarning("Self-dependency detected for task {TaskId}", newTaskOrUpdatedTask.Id);
            throw new SelfDependencyException("A task cannot depend on itself.");
        }

        var subgraphCache = new Dictionary<string, TaskItem>
        {
            [newTaskOrUpdatedTask.Id] = newTaskOrUpdatedTask
        };

        try
        {
            if (newTaskOrUpdatedTask.Dependencies.Count > 0)
            {
                var directDeps = await _repository.GetManyByIdsAsync(newTaskOrUpdatedTask.Dependencies, ct);
                foreach (var dep in directDeps)
                    subgraphCache[dep.Id] = dep;

                foreach (var depId in newTaskOrUpdatedTask.Dependencies)
                {
                    if (!subgraphCache.ContainsKey(depId))
                    {
                        _logger.LogWarning("Dependency {DepId} not found for task {TaskId}", depId, newTaskOrUpdatedTask.Id);
                        throw new DependencyNotFoundException($"Dependency task with ID {depId} does not exist.");
                    }
                }
            }

            var visited        = new HashSet<string>();
            var recursionStack = new HashSet<string>();
            var currentPath    = new List<string>();

            if (await HasCycleAsync(newTaskOrUpdatedTask.Id, subgraphCache, visited, recursionStack, currentPath, ct))
            {
                var pathString = string.Join(" ➔ ", currentPath);
                _logger.LogWarning("Circular dependency detected for task {TaskId}: {Path}", newTaskOrUpdatedTask.Id, pathString);
                throw new CircularDependencyException($"Circular dependency detected: {pathString}", new List<string>(currentPath));
            }

            _logger.LogInformation("Graph validation passed for task {TaskId} — subgraph size: {Size} node(s)",
                newTaskOrUpdatedTask.Id, subgraphCache.Count);
        }
        catch (DomainException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while validating the graph for task {TaskId}.", newTaskOrUpdatedTask.Id);
            throw new DomainException($"An unexpected error occurred during graph validation: {ex.Message}");
        }
    }

    
    private async Task<bool> HasCycleAsync(
        string currentId,
        Dictionary<string, TaskItem> cache,
        HashSet<string> visited,
        HashSet<string> recursionStack,
        List<string> currentPath,
        CancellationToken ct)
    {
        currentPath.Add(currentId);

        if (recursionStack.Contains(currentId))
        {
            int index = currentPath.IndexOf(currentId);
            var cycle = currentPath.Skip(index).ToList();
            currentPath.Clear();
            currentPath.AddRange(cycle);
            return true;
        }

        if (visited.Contains(currentId))
        {
            currentPath.RemoveAt(currentPath.Count - 1);
            return false;
        }

        visited.Add(currentId);
        recursionStack.Add(currentId);

        if (!cache.TryGetValue(currentId, out var currentTask))
        {
            var fetched = (await _repository.GetManyByIdsAsync(new[] { currentId }, ct)).FirstOrDefault();
            if (fetched != null)
            {
                cache[currentId] = fetched;
                currentTask = fetched;
            }
        }

        if (currentTask != null)
        {
            var uncachedDeps = currentTask.Dependencies.Where(d => !cache.ContainsKey(d)).ToList();
            if (uncachedDeps.Count > 0)
            {
                var fetchedDeps = await _repository.GetManyByIdsAsync(uncachedDeps, ct);
                foreach (var dep in fetchedDeps)
                    cache[dep.Id] = dep;
            }

            foreach (var depId in currentTask.Dependencies)
            {
                if (await HasCycleAsync(depId, cache, visited, recursionStack, currentPath, ct))
                    return true;
            }
        }

        recursionStack.Remove(currentId);
        currentPath.RemoveAt(currentPath.Count - 1);
        return false;
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

        var queue = new PriorityQueue<TaskItem, TaskItem>(new MyComparer());

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

    private class MyComparer : IComparer<TaskItem>
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

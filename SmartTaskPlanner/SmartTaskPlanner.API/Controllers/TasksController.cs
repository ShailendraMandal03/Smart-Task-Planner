using Microsoft.AspNetCore.Mvc;
using SmartTaskPlanner.Application.DTOs;
using SmartTaskPlanner.Application.Interfaces;

namespace SmartTaskPlanner.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TasksController> _logger;

    public TasksController(ITaskService taskService, ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult> GetTasks(
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        [FromQuery] bool all = false,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (all)
        {
            _logger.LogInformation("Fetching all tasks.");
            var tasks = await _taskService.GetAllTasksAsync(cancellationToken);
            _logger.LogInformation("Successfully fetched {Count} tasks.", tasks.Count());
            return Ok(tasks);
        }

        int size = pageSize ?? 5;
        _logger.LogInformation("Fetching paged tasks. Cursor: {Cursor}, PageSize: {PageSize}, Search: {Search}", cursor, size, search);
        var pagedResponse = await _taskService.GetPagedTasksAsync(cursor, size, search, cancellationToken);
        return Ok(pagedResponse);
    }

    [HttpGet("lookup")]
    public async Task<ActionResult<IEnumerable<TaskLookupDto>>> GetTaskLookup(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching task lookup data.");
        var lookup = await _taskService.GetTaskLookupAsync(cancellationToken);
        return Ok(lookup);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TaskResponseDto>> GetTaskById(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching task with ID: {Id}", id);
        var task = await _taskService.GetTaskByIdAsync(id, cancellationToken);
        _logger.LogInformation("Successfully fetched task with ID: {Id}", id);
        return Ok(task);
    }

    [HttpPost]
    public async Task<ActionResult<TaskResponseDto>> CreateTask([FromBody] CreateTaskDto dto, [FromQuery] bool force = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to create a task with Title: {Title}, Force: {Force}", dto.Title, force);
        var createdTask = await _taskService.CreateTaskAsync(dto, force, cancellationToken);
        _logger.LogInformation("Successfully created task with ID: {Id}", createdTask.Id);
        return CreatedAtAction(nameof(GetTaskById), new { id = createdTask.Id }, createdTask);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(string id, [FromBody] UpdateTaskDto dto, [FromQuery] bool force = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to update task with ID: {Id}, Force: {Force}", id, force);
        await _taskService.UpdateTaskAsync(id, dto, force, cancellationToken);
        _logger.LogInformation("Successfully updated task with ID: {Id}", id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to delete task with ID: {Id}", id);
        await _taskService.DeleteTaskAsync(id, cancellationToken);
        _logger.LogInformation("Successfully deleted task with ID: {Id}", id);
        return NoContent();
    }

    [HttpGet("plan")]
    public async Task<ActionResult<IEnumerable<TaskResponseDto>>> GetExecutionPlan(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating execution plan.");
        var plan = await _taskService.GenerateExecutionPlanAsync(cancellationToken);
        _logger.LogInformation("Successfully generated execution plan with {Count} tasks.", plan.Count());
        return Ok(plan);
    }
}

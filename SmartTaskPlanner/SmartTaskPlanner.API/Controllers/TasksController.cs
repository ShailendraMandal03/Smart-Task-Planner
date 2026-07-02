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
    public async Task<ActionResult> GetTasks([FromQuery] string? cursor, [FromQuery] int? pageSize, [FromQuery] bool all = false)
    {
        if (all)
        {
            _logger.LogInformation("Fetching all tasks.");
            var tasks = await _taskService.GetAllTasksAsync();
            _logger.LogInformation("Successfully fetched {Count} tasks.", tasks.Count());
            return Ok(tasks);
        }

        int size = pageSize ?? 5;
        _logger.LogInformation("Fetching paged tasks. Cursor: {Cursor}, PageSize: {PageSize}", cursor, size);
        var pagedResponse = await _taskService.GetPagedTasksAsync(cursor, size);
        return Ok(pagedResponse);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TaskResponseDto>> GetTaskById(string id)
    {
        _logger.LogInformation("Fetching task with ID: {Id}", id);
        var task = await _taskService.GetTaskByIdAsync(id);
        _logger.LogInformation("Successfully fetched task with ID: {Id}", id);
        return Ok(task);
    }

    [HttpPost]
    public async Task<ActionResult<TaskResponseDto>> CreateTask([FromBody] CreateTaskDto dto)
    {
        _logger.LogInformation("Attempting to create a task with Title: {Title}", dto.Title);
        var createdTask = await _taskService.CreateTaskAsync(dto);
        _logger.LogInformation("Successfully created task with ID: {Id}", createdTask.Id);
        return CreatedAtAction(nameof(GetTaskById), new { id = createdTask.Id }, createdTask);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(string id, [FromBody] UpdateTaskDto dto)
    {
        _logger.LogInformation("Attempting to update task with ID: {Id}", id);
        await _taskService.UpdateTaskAsync(id, dto);
        _logger.LogInformation("Successfully updated task with ID: {Id}", id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(string id)
    {
        _logger.LogInformation("Attempting to delete task with ID: {Id}", id);
        await _taskService.DeleteTaskAsync(id);
        _logger.LogInformation("Successfully deleted task with ID: {Id}", id);
        return NoContent();
    }

    [HttpGet("plan")]
    public async Task<ActionResult<IEnumerable<TaskResponseDto>>> GetExecutionPlan()
    {
        _logger.LogInformation("Generating execution plan.");
        var plan = await _taskService.GenerateExecutionPlanAsync();
        _logger.LogInformation("Successfully generated execution plan with {Count} tasks.", plan.Count());
        return Ok(plan);
    }
}

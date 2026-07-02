using Microsoft.AspNetCore.Mvc;
using SmartTaskPlanner.Domain.Exceptions;

namespace SmartTaskPlanner.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Instance = context.Request.Path
        };

        switch (exception)
        {
            case TaskNotFoundException ex:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                problemDetails.Title = "Not Found";
                problemDetails.Status = StatusCodes.Status404NotFound;
                problemDetails.Detail = ex.Message;
                break;

            case CircularDependencyException ex:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Circular Dependency Detected";
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Detail = ex.Message;
                break;

            case SelfDependencyException ex:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Invalid Dependency";
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Detail = ex.Message;
                break;
                
            case DependencyNotFoundException ex:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Dependency Not Found";
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Detail = ex.Message;
                break;

            case DomainException ex:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Domain Rule Violation";
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Detail = ex.Message;
                break;

            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "An internal server error occurred";
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Detail = exception.Message;
                break;
        }

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}

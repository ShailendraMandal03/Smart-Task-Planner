using SmartTaskPlanner.API.Middleware;
using SmartTaskPlanner.Application;
using SmartTaskPlanner.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();
// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS for Angular frontend.
// Origins are driven by configuration (appsettings.json "AllowedOrigins"),
// so they can be changed per environment without touching code.
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register Layer Dependencies
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// app.UseHttpsRedirection();

app.UseCors("AllowAngularDev");

app.UseAuthorization();

app.MapControllers();

try
{
    Log.Information("Starting web host");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

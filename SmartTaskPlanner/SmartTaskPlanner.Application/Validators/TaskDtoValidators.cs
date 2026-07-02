using FluentValidation;
using SmartTaskPlanner.Application.DTOs;

namespace SmartTaskPlanner.Application.Validators;

public class CreateTaskDtoValidator : AbstractValidator<CreateTaskDto>
{
    public CreateTaskDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.EstimatedEffort).GreaterThan(0);
        RuleFor(x => x.Dependencies).NotNull();
    }
}

public class UpdateTaskDtoValidator : AbstractValidator<UpdateTaskDto>
{
    public UpdateTaskDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.EstimatedEffort).GreaterThan(0);
        RuleFor(x => x.Dependencies).NotNull();
    }
}

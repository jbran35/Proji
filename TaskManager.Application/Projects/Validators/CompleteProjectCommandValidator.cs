using FluentValidation;
using TaskManager.Application.Projects.Commands;

namespace TaskManager.Application.Projects.Validators
{
    public class CompleteProjectCommandValidator : AbstractValidator<CompleteProjectCommand>
    {
        public CompleteProjectCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotNull()
                .NotEmpty()
                .WithMessage("Your ID Is Required To Complete This Project");

            RuleFor(x => x.ProjectId)
                .NotNull()
                .NotEmpty()
                .WithMessage("This Project's ID Is Required To Complete It");
        }
    }
}

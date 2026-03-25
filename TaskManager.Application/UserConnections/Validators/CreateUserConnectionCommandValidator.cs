using FluentValidation;
using TaskManager.Application.UserConnections.Commands;

namespace TaskManager.Application.UserConnections.Validators
{
    public class CreateUserConnectionCommandValidator : AbstractValidator<CreateUserConnectionCommand>
    { 
        public CreateUserConnectionCommandValidator() 
        {
            RuleFor(x => x.UserId)
                .NotNull()
                .NotEmpty()
                .WithMessage("Your User ID Must Be Provided To Add The Assignee");

            RuleFor(x => x.AssigneeId)
                .NotNull()
                .NotEmpty()
                .WithMessage("Assignee ID Required To Add To Your Group");
        }
    }
}
